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

// Assertion helpers (pure functions returning Results)
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
    suites
    |> List.map (fun s -> async { return runSuite s })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Array.toList

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
    | Some exp, Some act -> sprintf "  ✗ %s\n    Expected: %A\n    Actual:   %A" err.Message exp act
    | _ -> sprintf "  ✗ %s" err.Message

let formatDuration (ms: float) : string =
    if ms < 1.0 then sprintf "%.2fms" ms
    elif ms < 1000.0 then sprintf "%.0fms" ms
    else sprintf "%.2fs" (ms / 1000.0)

let formatOutcome outcome =
    match outcome with
    | Passed(name, duration) -> (sprintf "✓ %s (%s)" name (formatDuration duration), true)
    | Failed(name, errs, duration) ->
        let errMsg = errs |> List.map formatError |> String.concat "\n"
        (sprintf "✗ %s (%s)\n%s" name (formatDuration duration) errMsg, false)
    | Skipped(name, reason) -> (sprintf "⊘ %s (skipped: %s)" name reason, true)
    | Errored(name, ex, duration) ->
        (sprintf "✗ %s (%s)\n  Exception: %s" name (formatDuration duration) ex.Message, false)

let formatSuiteOutcome (so: SuiteOutcome) =
    let header =
        sprintf "\n=== %s (%s) ===" so.SuiteName (formatDuration so.TotalDurationMs)

    let results = so.Results |> List.map (formatOutcome >> fst) |> String.concat "\n"
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

    if allPassed then 0 else -1


let parseTestArgs (args: string array) (suites: TestSuite list) =

    let printHelp () =
        printfn "Usage:"
        printfn "  <no args>                   Run all suites sequentially"
        printfn "  --suite <name>              Run specific suite"
        printfn "  --test <suite> <test>       Run specific test"
        printfn "  --match <pattern>           Run tests matching pattern"
        printfn "  --parallel                  Run all suites in parallel"
        printfn "  --help, -h                  Show this help"
        0

    match args with
    | [| "--help" |]
    | [| "-h" |] -> printHelp ()

    | [| "--suite"; suiteName |] ->
        printfn "=== Running Suite: %s ===\n" suiteName
        runSuiteByName suiteName suites |> printResults

    | [| "--test"; suiteName; testName |] ->
        printfn "=== Running Test: %s -> %s ===\n" suiteName testName
        runSingleTest suiteName testName suites |> printResults

    | [| "--match"; pattern |] ->
        printfn "=== Running Tests Matching: %s ===\n" pattern
        runTestsMatching pattern suites |> printResults

    | [| "--parallel" |] ->
        printfn "=== Running All Suites (Parallel) ===\n"
        runAllSuitesParallel suites |> printResults

    | _ -> runAllSuites suites |> printResults
