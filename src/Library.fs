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

type TestSuite =
    { Name: string
      Tests: Test list
      Teardown: (unit -> unit) option }

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

/// Combine results, accumulating errors (skip takes precedence)
let combine r1 r2 =
    match r1, r2 with
    | Skip r, _
    | _, Skip r -> Skip r
    | Pass _, Pass _ -> Pass()
    | Fail e1, Pass _ -> Fail e1
    | Pass _, Fail e2 -> Fail e2
    | Fail e1, Fail e2 -> Fail(e1 @ e2)

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
let assertRegexp pattern str message =
    let regex = System.Text.RegularExpressions.Regex(pattern)

    match regex.IsMatch(string str) with
    | true -> pass
    | false -> fail (sprintf "%s: '%A' doesn't match pattern '%s'" message str pattern)

let assertNotRegexp pattern str message =
    let regex = System.Text.RegularExpressions.Regex(pattern)

    match regex.IsMatch(string str) with
    | true -> fail (sprintf "%s: '%A' should not match pattern '%s'" message str pattern)
    | false -> pass

// Test builders
let test name fn = { Name = name; Run = fn }

let testSkip name reason =
    { Name = name
      Run = fun () -> Skip reason }

let suite name tests =
    { Name = name
      Tests = tests
      Teardown = None }

/// Suite with buildup/teardown
/// buildup: unit -> 'env - Creates environment once
/// teardown: 'env -> unit - Cleans up environment once
/// tests: ('env -> Test) list - Each test receives the environment
let suiteWith name buildup teardown tests =
    let env = buildup ()

    { Name = name
      Tests = tests |> List.map (fun testBuilder -> testBuilder env)
      Teardown = Some(fun () -> teardown env) }

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

let runTest (t: Test) : TestOutcome =
    let sw = System.Diagnostics.Stopwatch.StartNew()

    try
        match t.Run() with
        | Pass _ ->
            sw.Stop()
            Passed(t.Name, sw.Elapsed.TotalMilliseconds)
        | Fail errs ->
            sw.Stop()
            Failed(t.Name, errs, sw.Elapsed.TotalMilliseconds)
        | Skip reason -> Skipped(t.Name, reason)
    with ex ->
        sw.Stop()
        Errored(t.Name, ex, sw.Elapsed.TotalMilliseconds)

let runSuite (s: TestSuite) : SuiteOutcome =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let results = s.Tests |> List.map runTest
    s.Teardown |> Option.iter (fun td -> td ())
    sw.Stop()

    { SuiteName = s.Name
      Results = results
      TotalDurationMs = sw.Elapsed.TotalMilliseconds }

// Sequential execution
let runAllSuites (suites: TestSuite list) : SuiteOutcome list = suites |> List.map runSuite

// Parallel execution - each suite runs in parallel
let runAllSuitesParallel (suites: TestSuite list) : SuiteOutcome list =
    suites |> Array.ofList |> Array.Parallel.map runSuite |> Array.toList

// Filtering - run specific tests or suites
let filterSuiteByName (suiteName: string) (suites: TestSuite list) : TestSuite list =
    suites |> List.filter (fun s -> s.Name = suiteName)

let filterTestByName (testName: string) (suite: TestSuite) : TestSuite =
    { suite with
        Tests = suite.Tests |> List.filter (fun t -> t.Name = testName) }

let filterTests (predicate: Test -> bool) (suite: TestSuite) : TestSuite =
    { suite with
        Tests = suite.Tests |> List.filter predicate }

// Run a single suite by name
let runSuiteByName (name: string) (suites: TestSuite list) : SuiteOutcome list =
    suites |> filterSuiteByName name |> runAllSuites

// Run a single test from a suite
let runSingleTest (suiteName: string) (testName: string) (suites: TestSuite list) : SuiteOutcome list =
    suites
    |> filterSuiteByName suiteName
    |> List.map (filterTestByName testName)
    |> runAllSuites

// Run tests matching a pattern
let runTestsMatching (pattern: string) (suites: TestSuite list) : SuiteOutcome list =
    suites
    |> List.map (filterTests (fun t -> t.Name.Contains(pattern)))
    |> runAllSuites

// Result formatting (pure)
let formatError (err: TestError) : string =
    match err.Expected, err.Actual with
    | Some exp, Some act -> sprintf "%2s%s\n%4sExpected: %A\n%4sActual:   %A" "" err.Message "" exp "" act
    | _ -> sprintf "%2s%s" "" err.Message

let formatDuration (ms: float) : string =
    if ms < 1.0 then sprintf "%.2fms" ms
    elif ms < 1000.0 then sprintf "%.0fms" ms
    else sprintf "%.2fs" (ms / 1000.0)

// ANSI color support
let supportsColor () =
    not (System.Console.IsOutputRedirected || System.Console.IsErrorRedirected)

let colorize colorCode text =
    if supportsColor () then
        sprintf "\u001b[%sm%s\u001b[0m" colorCode text
    else
        text

let getSymbol outcome =
    match outcome with
    | Passed _ -> colorize "32" "✓" // green
    | Failed _
    | Errored _ -> colorize "31" "✕" // red
    | Skipped _ -> colorize "90" "○" // gray

let formatOutcome outcome =
    match outcome with
    | Passed(name, duration) -> sprintf "%s (%s)" name (formatDuration duration)
    | Failed(name, errs, duration) ->
        let errMsg = errs |> List.map formatError |> String.concat "\n"
        sprintf "%s (%s)\n%s" name (formatDuration duration) errMsg
    | Skipped(name, reason) -> sprintf "%s (skipped: %s)" name reason
    | Errored(name, ex, duration) -> sprintf "%s (%s)\n%2sException: %s" name (formatDuration duration) "" ex.Message

let formatSuiteOutcome (so: SuiteOutcome) =
    let header =
        sprintf "\n=== %s (%s) ===" so.SuiteName (formatDuration so.TotalDurationMs)

    let results =
        so.Results
        |> List.map (fun r -> sprintf "%s %s" (getSymbol r) (formatOutcome r))
        |> String.concat "\n"

    sprintf "%s\n%s" header results

// Summary
let summarize (outcomes: SuiteOutcome list) =
    let allResults = outcomes |> List.collect (fun so -> so.Results)

    let passed =
        allResults
        |> List.filter (function
            | Passed _ -> true
            | _ -> false)
        |> List.length

    let failed =
        allResults
        |> List.filter (function
            | Failed _ -> true
            | _ -> false)
        |> List.length

    let skipped =
        allResults
        |> List.filter (function
            | Skipped _ -> true
            | _ -> false)
        |> List.length

    let errored =
        allResults
        |> List.filter (function
            | Errored _ -> true
            | _ -> false)
        |> List.length

    let total = passed + failed + errored + skipped
    let totalTime = outcomes |> List.sumBy (fun so -> so.TotalDurationMs)

    sprintf
        "\n%d/%d passed, %d failed, %d skipped, %d errored (Total: %s)"
        passed
        total
        failed
        skipped
        errored
        (formatDuration totalTime)


let printResults outcomes =
    outcomes |> List.iter (formatSuiteOutcome >> printfn "%s")

    printfn "%s" (summarize outcomes)

    let allPassed =
        outcomes
        |> List.forall (fun so ->
            so.Results
            |> List.forall (function
                | Passed _
                | Skipped _ -> true
                | _ -> false))

    if allPassed then 0 else 1

let summarizeJUnit (outcomes: SuiteOutcome list) : string =
    let escapeXml (s: string) =
        s
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;")

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
        let tests = so.Results.Length

        let failures =
            so.Results
            |> List.filter (function
                | Failed _ -> true
                | _ -> false)
            |> List.length

        let errors =
            so.Results
            |> List.filter (function
                | Errored _ -> true
                | _ -> false)
            |> List.length

        let skipped =
            so.Results
            |> List.filter (function
                | Skipped _ -> true
                | _ -> false)
            |> List.length

        let cases = so.Results |> List.map testcaseXml |> String.concat "\n"

        sprintf
            "%4s<testsuite name=\"%s\" tests=\"%d\" failures=\"%d\" errors=\"%d\" skipped=\"%d\" time=\"%s\">\n%s\n%4s</testsuite>"
            ""
            (escapeXml so.SuiteName)
            tests
            failures
            errors
            skipped
            (formatTime so.TotalDurationMs)
            cases
            ""

    let allResults = outcomes |> List.collect (fun so -> so.Results)
    let totalTests = allResults.Length

    let totalFailures =
        allResults
        |> List.filter (function
            | Failed _ -> true
            | _ -> false)
        |> List.length

    let totalErrors =
        allResults
        |> List.filter (function
            | Errored _ -> true
            | _ -> false)
        |> List.length

    let totalSkipped =
        allResults
        |> List.filter (function
            | Skipped _ -> true
            | _ -> false)
        |> List.length

    let totalTime = outcomes |> List.sumBy (fun so -> so.TotalDurationMs)
    let suites = outcomes |> List.map suiteXml |> String.concat "\n"

    sprintf
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<testsuites tests=\"%d\" failures=\"%d\" errors=\"%d\" skipped=\"%d\" time=\"%s\">\n%s\n</testsuites>"
        totalTests
        totalFailures
        totalErrors
        totalSkipped
        (formatTime totalTime)
        suites


let parseTestArgs (args: string array) (suites: TestSuite list) =

    let xmlFile =
        args
        |> Array.tryFindIndex ((=) "--xml")
        |> Option.bind (fun i -> Array.tryItem (i + 1) args)

    let args =
        match Array.tryFindIndex ((=) "--xml") args with
        | Some i -> Array.removeAt (i + 1) args |> Array.removeAt i
        | None -> args

    let outputResults (outcomes: SuiteOutcome list) =
        match xmlFile with
        | Some file ->
            System.IO.File.WriteAllText(file, summarizeJUnit outcomes)
            printfn "Results written to %s" file

            let allPassed =
                outcomes
                |> List.forall (fun so ->
                    so.Results
                    |> List.forall (function
                        | Passed _
                        | Skipped _ -> true
                        | _ -> false))

            if allPassed then 0 else 1
        | None -> printResults outcomes

    let printHelp () =
        printfn "Usage:"
        printfn "  <no args>                   Run all suites sequentially"
        printfn "  --suite <name>              Run specific suite"
        printfn "  --test <suite> <test>       Run specific test"
        printfn "  --match <pattern>           Run tests matching pattern"
        printfn "  --parallel                  Run all suites in parallel"
        printfn "  --xml <file>                Output results as JUnit XML to file"
        printfn "  --help, -h                  Show this help"
        0

    match args with
    | [| "--help" |]
    | [| "-h" |] -> printHelp ()
    | [| "--suite"; suiteName |] ->
        printfn "=== Running Suite: %s ===" suiteName
        runSuiteByName suiteName suites |> outputResults
    | [| "--test"; suiteName; testName |] ->
        printfn "=== Running Test: %s -> %s ===" suiteName testName
        runSingleTest suiteName testName suites |> outputResults
    | [| "--match"; pattern |] ->
        printfn "=== Running Tests Matching: %s ===" pattern
        runTestsMatching pattern suites |> outputResults
    | [| "--parallel" |] ->
        printfn "=== Running All Suites (Parallel) ==="
        runAllSuitesParallel suites |> outputResults
    | [||] -> runAllSuites suites |> outputResults
    | _ ->
        printfn "Unknown arguments. Use --help for usage."
        1
