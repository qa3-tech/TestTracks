module TestTracks

(*

    Railway-Oriented Testing Framework for F#
    Pure FP, no mutable state, explicit composition

    A purely functional testing framework using Railway-Oriented Programming principles.
    Tests are composable functions that return Results, making error handling explicit.

*)


// Core types
type TestError =
    { Message: string
      Expected: obj option
      Actual: obj option }

type SkipReason = string

type TestResult<'a> =
    | Pass of 'a
    | Fail of TestError list
    | Skip of SkipReason

type Test =
    { Name: string
      Run: unit -> TestResult<unit> }

/// Tests and teardown, available once setup has run.
type LoadedSuite =
    { Tests: Test list
      Teardown: unit -> unit }

/// Load runs setup lazily, inside the suite's run, so it runs on the suite's
/// thread and never runs when a filter excludes the suite by name.
type TestSuite =
    { Name: string
      Load: unit -> LoadedSuite }

/// Milliseconds on a monotonic clock. Injected so timing stays at the edge.
type Clock = unit -> float

[<RequireQualifiedAccess>]
type Filter =
    | All
    | Suite of suiteName: string
    | Test of suiteName: string * testName: string
    | Match of pattern: string

// Core test result builders
let pass = Pass()

let skip reason = Skip reason

let fail message =
    Fail
        [ { Message = message
            Expected = None
            Actual = None } ]

let failWith expected actual message =
    Fail
        [ { Message = message
            Expected = Some expected
            Actual = Some actual } ]

// Railway operators
let bind f result =
    match result with
    | Pass x -> f x
    | Fail e -> Fail e
    | Skip r -> Skip r

let map f result =
    match result with
    | Pass x -> Pass(f x)
    | Fail e -> Fail e
    | Skip r -> Skip r

/// Combine results, accumulating errors. Fail outranks Skip; Skip outranks Pass.
let combine first second =
    match first, second with
    | Fail firstErrors, Fail secondErrors -> Fail(firstErrors @ secondErrors)
    | Fail errors, _
    | _, Fail errors -> Fail errors
    | Skip reason, _
    | _, Skip reason -> Skip reason
    | Pass _, Pass _ -> Pass()

// Skip conditions (pure functions)
let skipIf condition reason =
    if condition then Skip reason else Pass()

let skipUnless condition reason =
    if not condition then Skip reason else Pass()

// Basic assertions (pure functions returning Results)
let assertEqual expected actual message =
    if expected = actual then
        pass
    else
        failWith expected actual message

let assertNotEqual unexpected actual message =
    if unexpected <> actual then
        pass
    else
        fail (sprintf "%s: got unexpected value %A" message actual)

let assertTrue condition message =
    if condition then pass else fail message

let assertFalse condition message =
    if not condition then pass else fail message

let assertSome option message =
    match option with
    | Some _ -> pass
    | None -> fail (sprintf "%s: expected Some, got None" message)

let assertNone option message =
    match option with
    | None -> pass
    | Some x -> fail (sprintf "%s: expected None, got Some %A" message x)

let assertOk result message =
    match result with
    | Ok _ -> pass
    | Error e -> fail (sprintf "%s: expected Ok, got Error %A" message e)

let assertError result message =
    match result with
    | Error _ -> pass
    | Ok x -> fail (sprintf "%s: expected Error, got Ok %A" message x)

// Nil checking
let assertNil value message =
    match box value with
    | null -> pass
    | _ -> fail (sprintf "%s: expected null, got %A" message value)

let assertNotNil value message =
    match box value with
    | null -> fail (sprintf "%s: expected non-null value" message)
    | _ -> pass

// Collections
let assertEmpty collection message =
    match box collection with
    | null -> pass
    | :? string as s when s.Length = 0 -> pass
    | :? string -> fail (sprintf "%s: expected empty, got %A" message collection)
    | :? System.Collections.ICollection as c when c.Count = 0 -> pass
    | :? System.Collections.ICollection -> fail (sprintf "%s: expected empty, got %A" message collection)
    | :? seq<obj> as s when Seq.isEmpty s -> pass
    | :? seq<obj> -> fail (sprintf "%s: expected empty, got %A" message collection)
    | _ -> fail (sprintf "%s: expected empty, got %A" message collection)

let assertNotEmpty collection message =
    match box collection with
    | null -> fail (sprintf "%s: should not be empty" message)
    | :? string as s when s.Length = 0 -> fail (sprintf "%s: should not be empty" message)
    | :? string -> pass
    | :? System.Collections.ICollection as c when c.Count = 0 -> fail (sprintf "%s: should not be empty" message)
    | :? System.Collections.ICollection -> pass
    | :? seq<obj> as s when Seq.isEmpty s -> fail (sprintf "%s: should not be empty" message)
    | :? seq<obj> -> pass
    | _ -> pass

let assertLen expected collection message =
    let getLen c =
        match box c with
        | :? string as s -> Some s.Length
        | :? System.Collections.ICollection as col -> Some col.Count
        | :? System.Collections.IEnumerable as e -> Some(e |> Seq.cast<obj> |> Seq.length)
        | _ -> None

    match getLen collection with
    | Some actual when actual = expected -> pass
    | Some actual -> failWith expected actual (sprintf "%s: length mismatch" message)
    | None -> fail (sprintf "%s: cannot determine length" message)

let assertContains element collection message =
    match box collection with
    | :? string as s when s.Contains(string element) -> pass
    | :? string -> fail (sprintf "%s: %A not found in %A" message element collection)
    | :? System.Collections.IEnumerable as e ->
        match e |> Seq.cast<obj> |> Seq.exists (fun x -> x = box element) with
        | true -> pass
        | false -> fail (sprintf "%s: %A not found in %A" message element collection)
    | _ -> fail (sprintf "%s: %A not found in %A" message element collection)

let assertNotContains element collection message =
    match box collection with
    | :? string as s when s.Contains(string element) ->
        fail (sprintf "%s: %A should not be in %A" message element collection)
    | :? string -> pass
    | :? System.Collections.IEnumerable as e ->
        match e |> Seq.cast<obj> |> Seq.exists (fun x -> x = box element) with
        | true -> fail (sprintf "%s: %A should not be in %A" message element collection)
        | false -> pass
    | _ -> pass

let assertSubset subset collection message =
    match box subset, box collection with
    | (:? System.Collections.IEnumerable as sub), (:? System.Collections.IEnumerable as col) ->
        let subList = sub |> Seq.cast<obj> |> Seq.toList
        let colList = col |> Seq.cast<obj> |> Seq.toList

        match subList |> List.forall (fun x -> List.exists ((=) x) colList) with
        | true -> pass
        | false -> fail (sprintf "%s: not all elements of subset found in collection" message)
    | _ -> fail (sprintf "%s: arguments must be collections" message)

let assertNotSubset subset collection message =
    match box subset, box collection with
    | (:? System.Collections.IEnumerable as sub), (:? System.Collections.IEnumerable as col) ->
        let subList = sub |> Seq.cast<obj> |> Seq.toList
        let colList = col |> Seq.cast<obj> |> Seq.toList

        match subList |> List.forall (fun x -> List.exists ((=) x) colList) with
        | true -> fail (sprintf "%s: subset should not be contained in collection" message)
        | false -> pass
    | _ -> fail (sprintf "%s: arguments must be collections" message)

let assertElementsMatch listA listB message =
    match box listA, box listB with
    | (:? System.Collections.IEnumerable as a), (:? System.Collections.IEnumerable as b) ->
        let aList = a |> Seq.cast<obj> |> Seq.toList
        let bList = b |> Seq.cast<obj> |> Seq.toList

        match aList.Length = bList.Length with
        | false -> failWith aList.Length bList.Length (sprintf "%s: different lengths" message)
        | true ->
            // For each element in A, find and remove from B
            let rec checkMatch remaining bRemaining =
                match remaining with
                | [] -> List.isEmpty bRemaining
                | x :: xs ->
                    match List.tryFindIndex ((=) x) bRemaining with
                    | Some idx ->
                        let newB = List.removeAt idx bRemaining
                        checkMatch xs newB
                    | None -> false

            match checkMatch aList bList with
            | true -> pass
            | false -> failWith aList bList (sprintf "%s: elements don't match" message)
    | _ -> fail (sprintf "%s: arguments must be collections" message)

// Numeric comparisons
let assertGreater actual expected message =
    match actual > expected with
    | true -> pass
    | false -> failWith (sprintf "> %A" expected) actual (sprintf "%s: not greater" message)

let assertGreaterOrEqual actual expected message =
    match actual >= expected with
    | true -> pass
    | false -> failWith (sprintf ">= %A" expected) actual (sprintf "%s: not greater or equal" message)

let assertLess actual expected message =
    match actual < expected with
    | true -> pass
    | false -> failWith (sprintf "< %A" expected) actual (sprintf "%s: not less" message)

let assertLessOrEqual actual expected message =
    match actual <= expected with
    | true -> pass
    | false -> failWith (sprintf "<= %A" expected) actual (sprintf "%s: not less or equal" message)

let assertInDelta expected actual delta message =
    let toFloat x =
        match box x with
        | :? int as i -> Some(float i)
        | :? int8 as i -> Some(float i)
        | :? int16 as i -> Some(float i)
        | :? int64 as i -> Some(float i)
        | :? uint as i -> Some(float i)
        | :? uint8 as i -> Some(float i)
        | :? uint16 as i -> Some(float i)
        | :? uint64 as i -> Some(float i)
        | :? float32 as f -> Some(float f)
        | :? float as f -> Some f
        | :? decimal as d -> Some(float d)
        | _ -> None

    match toFloat expected, toFloat actual with
    | Some e, Some a ->
        let diff = abs (e - a)

        match diff <= delta with
        | true -> pass
        | false -> fail (sprintf "%s: difference %A exceeds delta %A" message diff delta)
    | _ -> fail (sprintf "%s: arguments must be numeric types" message)


// Strings and Regex
/// An invalid pattern becomes an error value instead of an exception.
let private parseRegex (pattern: string) =
    try
        Ok(System.Text.RegularExpressions.Regex(pattern))
    with :? System.ArgumentException as error ->
        Error error.Message

let assertRegexp pattern str message =
    let text = string str

    match parseRegex pattern with
    | Ok regex when regex.IsMatch(text) -> pass
    | Ok _ -> fail (sprintf "%s: '%s' doesn't match pattern '%s'" message text pattern)
    | Error reason -> fail (sprintf "%s: invalid pattern '%s': %s" message pattern reason)

let assertNotRegexp pattern str message =
    let text = string str

    match parseRegex pattern with
    | Ok regex when regex.IsMatch(text) -> fail (sprintf "%s: '%s' should not match pattern '%s'" message text pattern)
    | Ok _ -> pass
    | Error reason -> fail (sprintf "%s: invalid pattern '%s': %s" message pattern reason)

// Test builders
let test name fn = { Name = name; Run = fn }

let testSkip name reason =
    { Name = name
      Run = fun () -> Skip reason }

let suite name tests =
    { Name = name
      Load = fun () -> { Tests = tests; Teardown = ignore } }

/// Suite with buildup/teardown
/// buildup: unit -> 'env - Creates environment once, when the suite runs
/// teardown: 'env -> unit - Cleans up environment once, after the last test
/// tests: ('env -> Test) list - Each test receives the environment
let suiteWith name buildup teardown tests =
    { Name = name
      Load =
        fun () ->
            let env = buildup ()

            { Tests = tests |> List.map (fun testBuilder -> testBuilder env)
              Teardown = fun () -> teardown env } }

// Computation expression for test composition
type TestBuilder() =
    member _.Return(x) = Pass x
    member _.ReturnFrom(x) = x
    member _.Bind(result, f) = bind f result
    member _.Zero() = pass
    member _.Combine(r1, r2) = combine r1 r2
    member _.Delay(f) = f
    member _.Run(f) = f ()

let test' = TestBuilder()

// Test runner with timing
type TestOutcome =
    | Passed of name: string * durationMs: float
    | Failed of name: string * errors: TestError list * durationMs: float
    | Skipped of name: string * reason: SkipReason
    | Errored of name: string * exn: exn * durationMs: float

type SuiteOutcome =
    { SuiteName: string
      Results: TestOutcome list
      TotalDurationMs: float }

/// Real monotonic clock in milliseconds.
let systemClock: Clock =
    fun () -> float (System.Diagnostics.Stopwatch.GetTimestamp()) * 1000.0 / float System.Diagnostics.Stopwatch.Frequency

let runTest (clock: Clock) (t: Test) : TestOutcome =
    let start = clock ()

    try
        match t.Run() with
        | Pass _ -> Passed(t.Name, clock () - start)
        | Fail errors -> Failed(t.Name, errors, clock () - start)
        | Skip reason -> Skipped(t.Name, reason)
    with ex ->
        Errored(t.Name, ex, clock () - start)

let private selected (filter: Filter) (suiteName: string) (testName: string) =
    match filter with
    | Filter.All -> true
    | Filter.Suite name -> name = suiteName
    | Filter.Test(wantedSuite, wantedTest) -> wantedSuite = suiteName && wantedTest = testName
    | Filter.Match pattern -> testName.Contains(pattern)

/// False when the filter excludes the suite by name, so setup can be skipped.
let private mayContainSelected (filter: Filter) (suiteName: string) =
    match filter with
    | Filter.Suite name
    | Filter.Test(name, _) -> name = suiteName
    | Filter.All
    | Filter.Match _ -> true

/// Runs the suite's selected tests in order. Setup runs only when the filter
/// may select a test in this suite; teardown runs whenever setup ran.
let runSuiteWith (clock: Clock) (filter: Filter) (s: TestSuite) : SuiteOutcome =
    let start = clock ()

    let results =
        if mayContainSelected filter s.Name then
            let loaded = s.Load()

            let outcomes =
                loaded.Tests
                |> List.filter (fun t -> selected filter s.Name t.Name)
                |> List.map (runTest clock)

            loaded.Teardown()
            outcomes
        else
            []

    { SuiteName = s.Name
      Results = results
      TotalDurationMs = clock () - start }

let private hasResults (outcome: SuiteOutcome) = not (List.isEmpty outcome.Results)

/// Suites with no selected tests are left out of the outcome.
let runSuites (clock: Clock) (filter: Filter) (suites: TestSuite list) : SuiteOutcome list =
    suites |> List.map (runSuiteWith clock filter) |> List.filter hasResults

/// Each suite runs on its own thread, setup included; tests within a suite stay sequential.
let runSuitesParallel (clock: Clock) (filter: Filter) (suites: TestSuite list) : SuiteOutcome list =
    suites
    |> Array.ofList
    |> Array.Parallel.map (runSuiteWith clock filter)
    |> Array.toList
    |> List.filter hasResults

let runSuite (s: TestSuite) : SuiteOutcome = runSuiteWith systemClock Filter.All s

// Sequential execution
let runAllSuites (suites: TestSuite list) : SuiteOutcome list = runSuites systemClock Filter.All suites

// Parallel execution - each suite runs in parallel
let runAllSuitesParallel (suites: TestSuite list) : SuiteOutcome list =
    runSuitesParallel systemClock Filter.All suites

// Filtering - run specific tests or suites
let filterSuiteByName (suiteName: string) (suites: TestSuite list) : TestSuite list =
    suites |> List.filter (fun s -> s.Name = suiteName)

let filterTests (predicate: Test -> bool) (s: TestSuite) : TestSuite =
    { s with
        Load =
            fun () ->
                let loaded = s.Load()

                { loaded with
                    Tests = loaded.Tests |> List.filter predicate } }

let filterTestByName (testName: string) (s: TestSuite) : TestSuite =
    filterTests (fun t -> t.Name = testName) s

// Run a single suite by name
let runSuiteByName (name: string) (suites: TestSuite list) : SuiteOutcome list =
    runSuites systemClock (Filter.Suite name) suites

// Run a single test from a suite
let runSingleTest (suiteName: string) (testName: string) (suites: TestSuite list) : SuiteOutcome list =
    runSuites systemClock (Filter.Test(suiteName, testName)) suites

// Run tests matching a pattern
let runTestsMatching (pattern: string) (suites: TestSuite list) : SuiteOutcome list =
    runSuites systemClock (Filter.Match pattern) suites

// Counting (pure)
type Counts =
    { Passed: int
      Failed: int
      Skipped: int
      Errored: int
      DurationMs: float }

let count (outcomes: SuiteOutcome list) : Counts =
    let results = outcomes |> List.collect (fun so -> so.Results)
    let countWhere predicate = results |> List.filter predicate |> List.length

    { Passed = countWhere (fun outcome -> outcome.IsPassed)
      Failed = countWhere (fun outcome -> outcome.IsFailed)
      Skipped = countWhere (fun outcome -> outcome.IsSkipped)
      Errored = countWhere (fun outcome -> outcome.IsErrored)
      DurationMs = outcomes |> List.sumBy (fun so -> so.TotalDurationMs) }

let exitCode (counts: Counts) = if counts.Failed + counts.Errored = 0 then 0 else 1

// Result formatting (pure)
let formatError (err: TestError) : string =
    match err.Expected, err.Actual with
    | Some exp, Some act -> sprintf "%2s%s\n%4sExpected: %A\n%4sActual:   %A" "" err.Message "" exp "" act
    | _ -> sprintf "%2s%s" "" err.Message

let formatDuration (ms: float) : string =
    if ms < 1.0 then sprintf "%.2fms" ms
    elif ms < 1000.0 then sprintf "%.0fms" ms
    else sprintf "%.2fs" (ms / 1000.0)

/// True when a test that ran took longer than the threshold.
let isSlow (slowMs: float option) (outcome: TestOutcome) =
    match slowMs, outcome with
    | None, _
    | _, Skipped _ -> false
    | Some threshold, (Passed(_, durationMs) | Failed(_, _, durationMs) | Errored(_, _, durationMs)) ->
        durationMs > threshold

// ANSI color support
let supportsColor () =
    not (System.Console.IsOutputRedirected || System.Console.IsErrorRedirected)

let colorize useColor colorCode text =
    if useColor then
        sprintf "\u001b[%sm%s\u001b[0m" colorCode text
    else
        text

let getSymbol useColor outcome =
    match outcome with
    | Passed _ -> colorize useColor "32" "✓" // green
    | Failed _
    | Errored _ -> colorize useColor "31" "✕" // red
    | Skipped _ -> colorize useColor "90" "○" // gray

let formatOutcome useColor (slowMs: float option) outcome =
    let duration durationMs =
        let marker =
            if isSlow slowMs outcome then
                " " + colorize useColor "33" "[slow]"
            else
                ""

        formatDuration durationMs + marker

    match outcome with
    | Passed(name, durationMs) -> sprintf "%s (%s)" name (duration durationMs)
    | Failed(name, errs, durationMs) ->
        let errMsg = errs |> List.map formatError |> String.concat "\n"
        sprintf "%s (%s)\n%s" name (duration durationMs) errMsg
    | Skipped(name, reason) -> sprintf "%s (skipped: %s)" name reason
    | Errored(name, ex, durationMs) -> sprintf "%s (%s)\n%2sException: %s" name (duration durationMs) "" ex.Message

let formatSuiteOutcome useColor (slowMs: float option) (so: SuiteOutcome) =
    let header =
        sprintf "\n=== %s (%s) ===" so.SuiteName (formatDuration so.TotalDurationMs)

    let results =
        so.Results
        |> List.map (fun r -> sprintf "%s %s" (getSymbol useColor r) (formatOutcome useColor slowMs r))
        |> String.concat "\n"

    sprintf "%s\n%s" header results

// Summary
let summarize (outcomes: SuiteOutcome list) =
    let counts = count outcomes
    let total = counts.Passed + counts.Failed + counts.Errored + counts.Skipped

    sprintf
        "\n%d/%d passed, %d failed, %d skipped, %d errored (Total: %s)"
        counts.Passed
        total
        counts.Failed
        counts.Skipped
        counts.Errored
        (formatDuration counts.DurationMs)

/// Appended to the summary only when a slow threshold is set.
let formatSlowSummary (slowMs: float option) (outcomes: SuiteOutcome list) =
    match slowMs with
    | Some threshold ->
        let slowCount =
            outcomes
            |> List.collect (fun so -> so.Results)
            |> List.filter (isSlow slowMs)
            |> List.length

        sprintf ", %d slow (> %s)" slowCount (formatDuration threshold)
    | None -> ""

let printResultsWith useColor (slowMs: float option) (outcomes: SuiteOutcome list) =
    outcomes |> List.iter (formatSuiteOutcome useColor slowMs >> printfn "%s")
    printfn "%s%s" (summarize outcomes) (formatSlowSummary slowMs outcomes)
    exitCode (count outcomes)

let printResults outcomes =
    printResultsWith (supportsColor ()) None outcomes

/// Escapes XML special characters. Control characters other than tab, newline and
/// carriage return are invalid in XML 1.0 even when escaped, so they become U+FFFD.
let private escapeXml (text: string) =
    let builder = System.Text.StringBuilder(text.Length)

    for character in text do
        match character with
        | '&' -> builder.Append("&amp;") |> ignore
        | '<' -> builder.Append("&lt;") |> ignore
        | '>' -> builder.Append("&gt;") |> ignore
        | '"' -> builder.Append("&quot;") |> ignore
        | '\'' -> builder.Append("&apos;") |> ignore
        | '\t'
        | '\n'
        | '\r' -> builder.Append(character) |> ignore
        | control when control < ' ' -> builder.Append('�') |> ignore
        | other -> builder.Append(other) |> ignore

    builder.ToString()

let summarizeJUnit (outcomes: SuiteOutcome list) : string =
    let formatTime (ms: float) = sprintf "%.3f" (ms / 1000.0)

    let testcaseXml (outcome: TestOutcome) =
        match outcome with
        | Passed(name, duration) ->
            sprintf "      <testcase name=\"%s\" time=\"%s\"/>" (escapeXml name) (formatTime duration)
        | Failed(name, errs, duration) ->
            let msg = errs |> List.map (fun e -> e.Message) |> String.concat "; " |> escapeXml
            let detail = errs |> List.map formatError |> String.concat "\n" |> escapeXml

            sprintf
                "      <testcase name=\"%s\" time=\"%s\">\n        <failure message=\"%s\" type=\"AssertionError\">%s</failure>\n      </testcase>"
                (escapeXml name)
                (formatTime duration)
                msg
                detail
        | Skipped(name, reason) ->
            sprintf
                "      <testcase name=\"%s\" time=\"0\">\n        <skipped message=\"%s\"/>\n      </testcase>"
                (escapeXml name)
                (escapeXml reason)
        | Errored(name, ex, duration) ->
            sprintf
                "      <testcase name=\"%s\" time=\"%s\">\n        <error message=\"%s\" type=\"%s\">%s</error>\n      </testcase>"
                (escapeXml name)
                (formatTime duration)
                (escapeXml ex.Message)
                (ex.GetType().Name)
                (escapeXml (string ex))

    let suiteXml (so: SuiteOutcome) =
        let counts = count [ so ]
        let cases = so.Results |> List.map testcaseXml |> String.concat "\n"

        sprintf
            "%4s<testsuite name=\"%s\" tests=\"%d\" failures=\"%d\" errors=\"%d\" skipped=\"%d\" time=\"%s\">\n%s\n%4s</testsuite>"
            ""
            (escapeXml so.SuiteName)
            so.Results.Length
            counts.Failed
            counts.Errored
            counts.Skipped
            (formatTime so.TotalDurationMs)
            cases
            ""

    let counts = count outcomes
    let total = counts.Passed + counts.Failed + counts.Errored + counts.Skipped
    let suites = outcomes |> List.map suiteXml |> String.concat "\n"

    sprintf
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<testsuites tests=\"%d\" failures=\"%d\" errors=\"%d\" skipped=\"%d\" time=\"%s\">\n%s\n</testsuites>"
        total
        counts.Failed
        counts.Errored
        counts.Skipped
        (formatTime counts.DurationMs)
        suites

// Command line (parsing is pure; parseTestArgs is the driver)
type RunOptions =
    { Filter: Filter
      Parallel: bool
      JUnitFile: string option
      /// Tests that take longer than this many milliseconds are marked slow.
      SlowMs: float option }

[<RequireQualifiedAccess>]
type Command =
    | Help
    | Run of RunOptions
    | Invalid of message: string

let helpText =
    "Usage:
  <no args>                   Run all suites sequentially
  --suite <name>              Run specific suite
  --test <suite> <test>       Run specific test
  --match <pattern>           Run tests matching pattern
  --parallel                  Run suites in parallel (combines with the above)
  --junit <file>              Write JUnit XML to file instead of console output (alias: --xml)
  --slow <ms>                 Mark tests slower than <ms> milliseconds
  --help, -h                  Show this help"

let defaultRunOptions =
    { Filter = Filter.All
      Parallel = false
      JUnitFile = None
      SlowMs = None }

/// Parses arguments after the program name. Flags combine in any order.
let parseArgs (arguments: string array) : Command =
    let mutable options = defaultRunOptions
    let mutable index = 0
    let mutable stopped = None

    // each pass consumes at least one argument or stops, so the loop is bounded
    while stopped.IsNone && index < arguments.Length do
        let remaining = arguments.Length - index

        match arguments[index] with
        | "--help"
        | "-h" -> stopped <- Some Command.Help
        | "--parallel" ->
            options <- { options with Parallel = true }
            index <- index + 1
        | "--suite" when remaining >= 2 ->
            options <- { options with Filter = Filter.Suite arguments[index + 1] }
            index <- index + 2
        | "--test" when remaining >= 3 ->
            options <- { options with Filter = Filter.Test(arguments[index + 1], arguments[index + 2]) }
            index <- index + 3
        | "--match" when remaining >= 2 ->
            options <- { options with Filter = Filter.Match arguments[index + 1] }
            index <- index + 2
        | "--junit"
        | "--xml" when remaining >= 2 ->
            options <- { options with JUnitFile = Some arguments[index + 1] }
            index <- index + 2
        | "--slow" when remaining >= 2 ->
            let text = arguments[index + 1]

            match
                System.Double.TryParse(
                    text,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture
                )
            with
            | true, milliseconds when milliseconds >= 0.0 ->
                options <- { options with SlowMs = Some milliseconds }
                index <- index + 2
            | _ -> stopped <- Some(Command.Invalid(sprintf "--slow needs a number of milliseconds, got: %s" text))
        | unknown ->
            stopped <- Some(Command.Invalid(sprintf "Unknown or incomplete argument: %s. Use --help for usage." unknown))

    defaultArg stopped (Command.Run options)

/// Entry point: parses arguments, runs, prints or writes the report. Returns 1 on any failure.
let parseTestArgs (args: string array) (suites: TestSuite list) : int =
    match parseArgs args with
    | Command.Help ->
        printfn "%s" helpText
        0
    | Command.Invalid message ->
        eprintfn "%s" message
        1
    | Command.Run options ->
        let run = if options.Parallel then runSuitesParallel else runSuites
        let outcomes = run systemClock options.Filter suites

        match options.JUnitFile with
        | Some path ->
            try
                System.IO.File.WriteAllText(path, summarizeJUnit outcomes)
                printfn "Results written to %s" path
                exitCode (count outcomes)
            with error ->
                eprintfn "Cannot write '%s': %s" path error.Message
                1
        | None -> printResultsWith (supportsColor ()) options.SlowMs outcomes
