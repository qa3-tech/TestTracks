# TestTracks: Railway-Oriented Testing Framework for F#

A purely functional testing framework using Railway-Oriented Programming (ROP) principles. Tests are composable functions that return `Result` types, making error handling explicit and composable.

## Philosophy

- **Pure Functions**: Tests are functions that return results, not side effects
- **Explicit Error Flow**: Errors are values on the railway, not exceptions
- **Composability**: Tests compose using standard FP operators (`bind`, `map`, `combine`)
- **No Magic**: Setup/teardown is explicit data flow, not hidden framework behavior
- **Type Safety**: Test environments are strongly typed and flow through your tests

## Installation

Reference the TestTracks DLL from your test project.

Requirements: 
- dotnet 8.0+

## Quick Start

### 1. Create Your Tests

```fsharp
// MyTests.fs
module MyTests

open TestTracks

let mathTests = suite "Math Tests" [
    test "addition works" (fun () ->
        assertEqual 4 (2 + 2) "should equal 4"
    )

    test "string length" (fun () ->
        assertEqual 5 "hello".Length "should be 5 chars"
    )
]

let validationTests = suite "Validation Tests" [
    test "positive numbers" (fun () ->
        assertTrue (5 > 0) "should be positive"
    )
]
```

### 2. Create Entry Point

```fsharp
// Program.fs
namespace MyProject.Tests

open TestTracks
open MyTests  // Your test modules

module Program =
    [<EntryPoint>]
    let main args =
        let allSuites = [ mathTests; validationTests ]
        parseTestArgs args allSuites
```

### 3. Run Tests

```bash
# Build
dotnet build

# Show help
./MyProject.Tests --help

# Run all tests
./MyProject.Tests

# Run specific suite
./MyProject.Tests --suite "Math Tests"

# Run tests matching pattern
./MyProject.Tests --match "validation"

# Run in parallel
./MyProject.Tests --parallel

# Generate JUnit XML report
./MyProject.Tests --xml results.xml
```

## Terminal Output

TestTracks provides colored output in modern terminals:
- **✓ Pass** = Green
- **✕ Fail/Error** = Red  
- **○ Skip** = Gray

Colors are automatically disabled when output is redirected (pipes, CI/CD logs), falling back to plain symbols for compatibility.

## Using TestTracks from C#

TestTracks is primarily designed for F#, but includes a C# wrapper (`TestTracksCSharp`) that provides an idiomatic C# API. The core principles remain the same: tests are pure functions that return results.

### API Mapping

| F#                                         | C#                                                |
| ------------------------------------------ | ------------------------------------------------- |
| `test "name" (fun () -> ...)`              | `Tests.Create("name", () => ...)`                 |
| `testSkip "name" "reason"`                 | `Tests.Skip("name", "reason")`                    |
| `suite "name" [...]`                       | `Suites.Create("name", ...)`                      |
| `suiteWith "name" setup teardown [...]`    | `Suites.CreateWith("name", setup, teardown, ...)` |
| `assertEqual expected actual msg`          | `Asserts.Equal(expected, actual, msg)`            |
| `assertTrue cond msg`                      | `Asserts.True(cond, msg)`                         |
| `assertFalse cond msg`                     | `Asserts.False(cond, msg)`                        |
| `assertSome opt msg`                       | `Asserts.Some(opt, msg)`                          |
| `assertNone opt msg`                       | `Asserts.None(opt, msg)`                          |
| `assertOk result msg`                      | `Asserts.Ok(result, msg)`                         |
| `assertError result msg`                   | `Asserts.Error(result, msg)`                      |
| `assertEmpty coll msg`                     | `Asserts.Empty(coll, msg)`                        |
| `assertNotEmpty coll msg`                  | `Asserts.NotEmpty(coll, msg)`                     |
| `assertLen len coll msg`                   | `Asserts.Len(len, coll, msg)`                     |
| `assertContains elem coll msg`             | `Asserts.Contains(elem, coll, msg)`               |
| `assertNotContains elem coll msg`          | `Asserts.NotContains(elem, coll, msg)`            |
| `assertSubset sub coll msg`                | `Asserts.Subset(sub, coll, msg)`                  |
| `assertNotSubset sub coll msg`             | `Asserts.NotSubset(sub, coll, msg)`               |
| `assertElementsMatch a b msg`              | `Asserts.ElementsMatch(a, b, msg)`                |
| `assertGreater actual expected msg`        | `Asserts.Greater(actual, expected, msg)`          |
| `assertGreaterOrEqual actual expected msg` | `Asserts.GreaterOrEqual(actual, expected, msg)`   |
| `assertLess actual expected msg`           | `Asserts.Less(actual, expected, msg)`             |
| `assertLessOrEqual actual expected msg`    | `Asserts.LessOrEqual(actual, expected, msg)`      |
| `assertInDelta expected actual delta msg`  | `Asserts.InDelta(expected, actual, delta, msg)`   |
| `assertRegexp pattern str msg`             | `Asserts.Regexp(pattern, str, msg)`               |
| `assertNotRegexp pattern str msg`          | `Asserts.NotRegexp(pattern, str, msg)`            |
| `assertNil value msg`                      | `Asserts.Nil(value, msg)`                         |
| `assertNotNil value msg`                   | `Asserts.NotNil(value, msg)`                      |
| `combine r1 r2`                            | `Outcomes.And(r1, r2)`                            |
| `r1 \|> combine r2 \|> combine r3`         | `Outcomes.All(r1, r2, r3)`                        |
| `skipIf cond reason`                       | `Guards.When(cond, reason, () => ...)`            |
| `skipUnless cond reason`                   | `Guards.Unless(cond, reason, () => ...)`          |
| `parseTestArgs args suites`                | `TestTracks.parseTestArgs(args, ListModule.OfSeq(suites))` |

### Quick C# Example

```csharp
using System;
using TestTracksCSharp;
using Microsoft.FSharp.Collections;

namespace MyProject.Tests
{
    public static class MyTests
    {
        public static TestTracks.TestSuite BasicTests => Suites.Create("Basic Math",
            Tests.Create("addition works", () =>
                Asserts.Equal(4, 2 + 2, "two plus two")),

            Tests.Create("multiplication works", () =>
                Asserts.Equal(42, 6 * 7, "six times seven")),

            Tests.Skip("division by zero", "not implemented yet")
        );
    }

    class Program
    {
        static int Main(string[] args)
        {
            var allSuites = new[] { MyTests.BasicTests };
            return TestTracks.parseTestArgs(
                args,
                ListModule.OfSeq(allSuites)
            );
        }
    }
}
```

### Key Differences from F#

#### 1. No Computation Expression

C# doesn't have F#'s `test'` computation expression. To compose dependent assertions (short-circuit on failure), nest them manually:

**F# (with computation expression):**

```fsharp
test "dependent checks" (fun () -> test' {
    do! assertTrue (x > 0) "must be positive"
    do! assertTrue (x < 100) "must be under 100"
    return! assertEqual 42 x "should be 42"
})
```

**C# (manual composition):**

```csharp
Tests.Create("dependent checks", () =>
{
    var result = Asserts.True(x > 0, "must be positive");
    if (Outcomes.IsFailed(result)) return result;

    result = Asserts.True(x < 100, "must be under 100");
    if (Outcomes.IsFailed(result)) return result;

    return Asserts.Equal(42, x, "should be 42");
})
```

For most cases, use `Outcomes.All()` to run all checks and accumulate errors:

```csharp
Tests.Create("validate order", () =>
    Outcomes.All(
        Asserts.True(order.Total > 0, "total positive"),
        Asserts.True(order.Items.Count > 0, "has items"),
        Asserts.True(order.CustomerId.HasValue, "has customer")
    )
)
```

#### 2. Use Records for Test Environments

C# records work perfectly for test environments:

```csharp
public record TestEnv(string TempDir, DateTime StartTime);

public static TestTracks.TestSuite FileTests => Suites.CreateWith(
    "File Operations",
    setup: () => new TestEnv(CreateTempDir(), DateTime.Now),
    teardown: env => CleanupTempDir(env.TempDir),

    env => Tests.Create("can write", () =>
    {
        var file = Path.Combine(env.TempDir, "test.txt");
        File.WriteAllText(file, "hello");
        return Asserts.True(File.Exists(file), "file exists");
    }),

    env => Tests.Create("can read", () =>
    {
        var file = Path.Combine(env.TempDir, "test.txt");
        File.WriteAllText(file, "hello");
        var content = File.ReadAllText(file);
        return Asserts.Equal("hello", content, "reads content");
    })
);
```

#### 3. Variadic Arguments

`Suites.Create()` and `Outcomes.All()` accept `params`, so you can pass any number of tests:

```csharp
Suites.Create("My Suite",
    Tests.Create("test 1", () => ...),
    Tests.Create("test 2", () => ...),
    Tests.Create("test 3", () => ...)
)

Outcomes.All(
    Asserts.Equal(1, x, "check 1"),
    Asserts.Equal(2, y, "check 2"),
    Asserts.Equal(3, z, "check 3")
)
```

#### 4. Skip Guards

Use `Guards.When` and `Guards.Unless` for conditional skips:

```csharp
Tests.Create("windows only", () =>
    Guards.Unless(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        "Windows only test",
        () => Asserts.True(true, "test logic")
    )
)

Tests.Create("skip in CI", () =>
{
    var isCI = Environment.GetEnvironmentVariable("CI") != null;
    return Guards.When(
        !isCI,
        "too slow for CI",
        () => Asserts.True(true, "test logic")
    );
})
```

### Complete C# Example

```csharp
using System;
using System.IO;
using TestTracksCSharp;
using Microsoft.FSharp.Collections;

namespace MyTests
{
    // Define your test environment
    public record TestEnv(string TempDir, DateTime StartTime);

    public static class Examples
    {
        // Suite with setup/teardown
        public static TestTracks.TestSuite FileTests => Suites.CreateWith(
            "File Operations",
            setup: () =>
            {
                var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(dir);
                Console.WriteLine($"Created temp dir: {dir}");
                return new TestEnv(dir, DateTime.Now);
            },
            teardown: env =>
            {
                Directory.Delete(env.TempDir, true);
                Console.WriteLine("Cleaned up temp dir");
            },
            env => Tests.Create("can create file", () =>
            {
                var file = Path.Combine(env.TempDir, "test.txt");
                File.WriteAllText(file, "hello");
                return Asserts.True(File.Exists(file), "file should exist");
            }),
            env => Tests.Create("can read file", () =>
            {
                var file = Path.Combine(env.TempDir, "test.txt");
                File.WriteAllText(file, "hello");
                var content = File.ReadAllText(file);
                return Asserts.Equal("hello", content, "should read content");
            }),
            env => Tests.Skip("performance test", "too slow")
        );

        // Simple suite without setup
        public static TestTracks.TestSuite MathTests => Suites.Create("Math",
            Tests.Create("addition", () =>
                Asserts.Equal(4, 2 + 2, "should add")),

            Tests.Create("composition", () =>
                Outcomes.All(
                    Asserts.True(2 > 0, "positive"),
                    Asserts.False(2 < 0, "not negative")
                ))
        );
    }

    class Program
    {
        static int Main(string[] args)
        {
            var allSuites = new[] { Examples.FileTests, Examples.MathTests };
            return TestTracks.parseTestArgs(
                args,
                ListModule.OfSeq(allSuites)
            );
        }
    }
}
```

### Data-Driven Tests in C#

Use LINQ to generate tests from data:

```csharp
public static TestTracks.TestSuite DataDrivenTests
{
    get
    {
        var testCases = new[] { (2, 4), (5, 10), (10, 20), (0, 0), (-5, -10) };

        var tests = testCases.Select(c =>
            Tests.Create($"{c.Item1} * 2 = {c.Item2}", () =>
                Asserts.Equal(c.Item2, c.Item1 * 2, "should double"))
        );

        return Suites.Create("Data-Driven Tests", tests.ToArray());
    }
}
```

### Accumulating vs Short-Circuiting

**Accumulate all errors** (all assertions run):

```csharp
Tests.Create("validate order", () =>
    Outcomes.All(
        Asserts.True(order.Total > 0m, "total positive"),
        Asserts.True(order.Items.Count > 0, "has items"),
        Asserts.True(order.CustomerId.HasValue, "has customer")
    )
)
// If all fail, you see ALL three errors
```

**Short-circuit on first failure** (stops at first error):

```csharp
Tests.Create("dependent checks", () =>
{
    var result = Asserts.Some(user, "user exists");
    if (Outcomes.IsFailed(result)) return result;

    // Only runs if user exists
    return Asserts.Equal("alice", user.Value.Name, "name matches");
})
```

### Running Tests

```bash
# Build
dotnet build

# Run all tests
./MyProject.Tests

# Run specific suite
./MyProject.Tests --suite "File Operations"

# Run in parallel
./MyProject.Tests --parallel

# Generate XML report
./MyProject.Tests --xml results.xml
```

### C# API Namespaces

```csharp
using TestTracksCSharp;  // Core wrapper types

// Available static classes:
Tests      // Create, Skip
Suites     // Create, CreateWith
Asserts    // Equal, True, False, Some, None, etc.
Outcomes   // And, All, IsFailed, IsSkipped
Guards     // When, Unless
Printer    // Print, WriteJUnitXml
Runner     // Suite, All, Parallel
```

See the full F# documentation below for detailed explanations of concepts like Railway-Oriented Programming, setup/teardown patterns, and best practices—all of which apply equally to C#.

## Core Concepts

### 1. Test Results (The Railway)

Tests return one of three results:

```fsharp
type TestResult<'a> =
    | Pass of 'a      // Success track
    | Fail of errors  // Failure track
    | Skip of reason  // Skip track
```

### 2. Test Builders

```fsharp
// Simple test
test "name" (fun () -> assertEqual expected actual "message")

// Skip a test
testSkip "not ready" "waiting for API v2"

// Suite of tests (runs in order)
suite "Suite Name" [
    test "first" (fun () -> ...)
    test "second" (fun () -> ...)
]
```

### 3. Assertions (Pure Functions)

All assertions return `TestResult`:

**Equality:**

```fsharp
assertEqual expected actual "message"
assertNotEqual unexpected actual "message"
```

**Boolean:**

```fsharp
assertTrue condition "message"
assertFalse condition "message"
```

**Option:**

```fsharp
assertSome option "message"
assertNone option "message"
```

**Result:**

```fsharp
assertOk result "message"
assertError result "message"
```

**Nil Checking:**

```fsharp
assertNil value "message"
assertNotNil value "message"
```

**Collections:**

```fsharp
assertEmpty collection "message"
assertNotEmpty collection "message"
assertLen expectedLength collection "message"
assertContains element collection "message"
assertNotContains element collection "message"
assertSubset subset collection "message"
assertNotSubset subset collection "message"
assertElementsMatch listA listB "message"  // Same elements, any order
```

**Numeric Comparisons:**

```fsharp
assertGreater actual expected "message"
assertGreaterOrEqual actual expected "message"
assertLess actual expected "message"
assertLessOrEqual actual expected "message"
assertInDelta expected actual delta "message"  // For floating-point comparisons
```

**Strings & Regex:**

```fsharp
assertRegexp pattern str "message"
assertNotRegexp pattern str "message"
```

### 4. Railway Composition

Use the `test'` computation expression for clean composition:

```fsharp
test "composed checks" (fun () -> test' {
    let! value = someFunction() |> assertOk "should succeed"
    do! skipIf (value < 0) "negative values not supported"
    return! assertEqual 42 value "should be 42"
})
```

Or use explicit combinators:

```fsharp
test "manual composition" (fun () ->
    assertTrue (x > 0) "positive"
    |> combine (assertEqual 10 x "equals 10")
    |> combine (assertSome (Some x) "is Some")
)
```

### `let` vs `let!` in `test'`

**Important:** `let!` only works with `TestResult`, not F#'s built-in `Result`.

```fsharp
// WRONG: Result<int,'a> is not TestResult<'a>
let! result = Ok 10 |> Result.map ((*) 2)  // Type error!

// RIGHT: Use plain 'let' for Result values
let result = Ok 10 |> Result.map ((*) 2)
return! assertOk result "should succeed"
```

Use `let!` when you have a `TestResult` and want to short-circuit on failure:

```fsharp
test "dependent steps" (fun () -> test' {
    do! assertTrue (x > 0) "must be positive"  // TestResult, short-circuits
    let doubled = x * 2                         // plain value, always runs
    return! assertEqual 10 doubled "should be 10"
})
```

## Setup and Teardown

### Suite-Level Environment (Buildup/Teardown)

Create shared environment for all tests in a suite:

**F# Example:**

```fsharp
type MyEnv = {
    Database: Connection
    Config: Settings
    OutputFile: string
}

let myTests = suiteWith "Database Tests"
    // Buildup: runs ONCE before all tests
    (fun () ->
        printfn "Setting up..."
        {
            Database = openConnection()
            Config = loadConfig()
            OutputFile = createTempFile()
        }
    )
    // Teardown: runs ONCE after all tests
    (fun env ->
        printfn "Cleaning up..."
        closeConnection env.Database
        deleteFile env.OutputFile
    )
    // Tests: each receives the environment
    [
        fun env -> test "can query database" (fun () ->
            let result = query env.Database "SELECT 1"
            assertOk result "query should succeed"
        )

        fun env -> test "can write to file" (fun () ->
            File.AppendAllText(env.OutputFile, "test data")
            assertTrue (File.Exists(env.OutputFile)) "file should exist"
        )
    ]
```

**Key Points:**

- Buildup runs **once** and returns a value of any type
- All tests receive this **same value** as a parameter
- Tests run **sequentially** in array order
- Teardown runs **once** at the end with the same value

### Without Setup/Teardown

For simple tests, just use `suite`:

```fsharp
let simpleTests = suite "Simple Tests" [
    test "no setup needed" (fun () ->
        assertEqual 42 (21 * 2) "math works"
    )
]
```

## Skip Directives

### Skip Entire Test

```fsharp
testSkip "not implemented" "waiting for feature X"
```

### Conditional Skip

```fsharp
test "windows only" (fun () -> test' {
    do! skipUnless isWindows "Windows only test"
    return! assertTrue true "test logic"
})

test "skip in CI" (fun () -> test' {
    let isCI = System.Environment.GetEnvironmentVariable("CI") <> null
    do! skipIf isCI "too slow for CI"
    return! assertTrue true "test logic"
})
```

## Data-Driven Testing

Use the test environment to pass arrays of test data. This gives you property-based testing without additional frameworks!

### Pattern 1: Simple Test Cases

**F# Example:**

```fsharp
let dataDrivenTests =
    let testCases = [
        (2, 4)
        (5, 10)
        (10, 20)
        (0, 0)
        (-5, -10)
    ]

    suite "Data-Driven Tests" [
        // Generate a test for each case
        for (input, expected) in testCases do
            test (sprintf "%d * 2 = %d" input expected) (fun () ->
                assertEqual expected (input * 2) "should double"
            )
    ]

// Output:
// ✓ 2 * 2 = 4 (0.12ms)
// ✓ 5 * 2 = 10 (0.08ms)
// ✓ 10 * 2 = 20 (0.09ms)
// ✓ 0 * 2 = 0 (0.07ms)
// ✓ -5 * 2 = -10 (0.11ms)
```

### Pattern 2: Using Environment for Complex Data

**F# Example:**

```fsharp
type User = { Name: string; Age: int; Email: string }

type TestEnv = {
    ValidUsers: User list
    InvalidUsers: User list
}

let userTests = suiteWith "User Validation"
    (fun () ->
        {
            ValidUsers = [
                { Name = "Alice"; Age = 30; Email = "alice@example.com" }
                { Name = "Bob"; Age = 25; Email = "bob@example.com" }
            ]
            InvalidUsers = [
                { Name = ""; Age = 30; Email = "invalid@example.com" }
                { Name = "Charlie"; Age = -5; Email = "charlie@example.com" }
            ]
        }
    )
    (fun env -> ())
    [
        // Individual test for each valid user
        fun env -> suite "Valid Users" [
            for user in env.ValidUsers do
                test (sprintf "validates %s" user.Name) (fun () ->
                    assertTrue (validateUser user) "should be valid"
                )
        ] |> fun s -> s.Tests.Head

        // Single test that runs all cases and collects errors
        fun env -> test "all invalid users rejected" (fun () ->
            env.InvalidUsers
            |> List.map (fun user ->
                assertFalse (validateUser user)
                    (sprintf "%s should be invalid" user.Name)
            )
            |> List.reduce combine  // Accumulates all errors!
        )
    ]
```

### Pattern 3: Collecting All Errors with `combine`

When you want to see **all failures** in a single test run, use `combine`:

```fsharp
test "validate all properties" (fun () ->
    // These ALL run, even if some fail
    assertTrue (order.Total > 0) "total positive"
    |> combine (assertTrue (order.Items.Length > 0) "has items")
    |> combine (assertSome order.CustomerId "has customer")
    |> combine (assertTrue order.IsValid "is valid")
)

// If all fail, you'll see ALL four errors:
// ✕ validate all properties (2.31ms)
//   total positive
//   has items
//   has customer
//   is valid
```

### Pattern 4: Reading Test Data from Files

```fsharp
let fileBasedTests = suiteWith "CSV Tests"
    (fun () ->
        let csv = File.ReadAllLines("testdata.csv")
        let cases =
            csv
            |> Array.skip 1  // Skip header
            |> Array.map (fun line ->
                let parts = line.Split(',')
                (int parts.[0], int parts.[1], int parts.[2])
            )
            |> Array.toList
        { TestCases = cases }
    )
    (fun env -> ())
    [
        fun env -> suite "CSV Cases" [
            for (a, b, expected) in env.TestCases do
                test (sprintf "%d + %d = %d" a b expected) (fun () ->
                    assertEqual expected (a + b) "should add"
                )
        ] |> fun s -> s.Tests.Head
    ]
```

### Short-Circuit vs Accumulate Errors

**Short-circuit** (stop at first failure):

```fsharp
test "dependent checks" (fun () -> test' {
    let! user = findUser id |> assertSome "user exists"
    // Only runs if user exists
    return! assertEqual "alice" user.Name "name matches"
})

// Output if user not found:
// ✕ dependent checks (1.2ms)
//   user exists: expected Some, got None
```

**Accumulate** (collect all failures):

```fsharp
test "independent checks" (fun () ->
    assertSome user "user exists"
    |> combine (assertEqual "alice" user.Name "name matches")
    |> combine (assertEqual 30 user.Age "age matches")
)

// Output if all fail:
// ✕ independent checks (1.5ms)
//   user exists: expected Some, got None
//   name matches: expected "alice", got "bob"
//   age matches: expected 30, got 25
```

**Key difference:**

- `test'` with `let!` stops at first error (good for dependent checks)
- `combine` evaluates everything and merges errors (good for independent checks)

## Parallel vs Sequential Execution

### Sequential (Default)

All suites run one after another:

```fsharp
let results = runAllSuites [suite1; suite2; suite3]
```

### Parallel

Each suite runs in parallel (tests within a suite are still sequential):

```fsharp
let results = runAllSuitesParallel [suite1; suite2; suite3]
```

### Running Specific Tests

```fsharp
// Run only one suite
runSuiteByName "Database Tests" allSuites

// Run a single test from a suite
runSingleTest "Database Tests" "can query database" allSuites

// Run all tests matching a pattern
runTestsMatching "database" allSuites  // Matches any test with "database" in name
```

### Command Line Options

```bash
./MyTests --help                              # Show help
./MyTests                                     # Run all tests
./MyTests --suite "Database Tests"            # Run specific suite
./MyTests --test "Database Tests" "can query" # Run specific test
./MyTests --match "validation"                # Run tests matching pattern
./MyTests --parallel                          # Run in parallel
./MyTests --xml results.xml                   # Generate JUnit XML report
```

**When to use parallel:**

- Suites are independent (no shared state)
- Each suite has its own resources (separate databases, files, etc.)
- You want faster test execution

**When to use sequential:**

- Suites share resources
- Order matters across suites
- Easier debugging

## Timing Information

All test results include timing:

```
=== Database Tests (245.32ms) ===
✓ can query database (12.45ms)
✓ can write to file (3.21ms)
✕ validation fails (189.03ms)
  Expected: 42
    Actual:   0

15/18 passed, 2 failed, 1 skipped, 0 errored (Total: 1.23s)
```

## Complete Example (F#)

```fsharp
module MyTests =
    open TestTracks
    open System.IO

    // Define your test environment
    type TestEnv = {
        TempDir: string
        StartTime: System.DateTime
    }

    // Suite with setup/teardown
    let fileTests = suiteWith "File Operations"
        (fun () ->
            let dir = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString())
            Directory.CreateDirectory(dir) |> ignore
            printfn "Created temp dir: %s" dir
            { TempDir = dir; StartTime = System.DateTime.Now }
        )
        (fun env ->
            Directory.Delete(env.TempDir, true)
            printfn "Cleaned up temp dir"
        )
        [
            fun env -> test "can create file" (fun () ->
                let file = Path.Combine(env.TempDir, "test.txt")
                File.WriteAllText(file, "hello")
                assertTrue (File.Exists(file)) "file should exist"
            )

            fun env -> test "can read file" (fun () -> test' {
                let file = Path.Combine(env.TempDir, "test.txt")
                File.WriteAllText(file, "hello")
                let content = File.ReadAllText(file)
                return! assertEqual "hello" content "should read content"
            })

            fun env -> testSkip "performance test" "too slow"
        ]

    // Simple suite without setup
    let mathTests = suite "Math" [
        test "addition" (fun () ->
            assertEqual 4 (2 + 2) "should add"
        )

        test "composition" (fun () ->
            assertTrue (2 > 0) "positive"
            |> combine (assertFalse (2 < 0) "not negative")
        )
    ]

    [<EntryPoint>]
    let main args =
        let allSuites = [fileTests; mathTests]
        parseTestArgs args allSuites
```

## Best Practices

### 1. Keep Tests Pure

```fsharp
// Good: Pure function, returns result
test "validate email" (fun () ->
    let result = validateEmail "test@example.com"
    assertOk result "should be valid"
)

// Avoid: Side effects in test body
test "send email" (fun () ->
    sendEmailNow()  // Side effect!
    assertTrue true "sent"
)
```

### 2. Use Buildup for Shared Resources

```fsharp
// Good: Share database connection
suiteWith "DB Tests"
    (fun () -> openDB())
    closeDB
    [fun db -> test "query" ...]

// Avoid: Opening connection in each test
suite "DB Tests" [
    test "query" (fun () ->
        let db = openDB()  // Repeated!
        ...
    )
]
```

### 3. Make Failures Clear

```fsharp
// Good: Descriptive message
assertEqual 42 actual "user age should be 42"

// Avoid: Generic message
assertEqual 42 actual "wrong"
```

### 4. Test One Thing

```fsharp
// Good: Focused test
test "validates positive numbers" (fun () ->
    assertTrue (validate 5) "should accept positive"
)

test "rejects negative numbers" (fun () ->
    assertFalse (validate -5) "should reject negative"
)

// Avoid: Testing too much
test "validation" (fun () ->
    assertTrue (validate 5) "positive"
    |> combine (assertFalse (validate -5) "negative")
    |> combine (assertTrue (validate 0) "zero")
    |> combine (assertEqual "error" (validate 999) "large")
)
```

### 5. Use Skips Wisely

```fsharp
// Good: Clear reason
testSkip "performance benchmark" "requires production data - run manually"

// Avoid: Vague skip
testSkip "broken" "doesn't work"
```

### 6. Choose the Right Assertion

```fsharp
// Good: Specific assertion for collections
assertContains 3 [1; 2; 3] "should contain 3"
assertLen 5 list "should have 5 elements"
assertElementsMatch [1; 2; 3] [3; 2; 1] "same elements"

// Less clear: Generic assertions
assertTrue (List.contains 3 [1; 2; 3]) "should contain 3"
assertTrue (list.Length = 5) "should have 5 elements"
```

### 7. Use assertInDelta for Floating-Point

```fsharp
// Good: Account for floating-point precision
assertInDelta 3.14159 actual 0.0001 "should be close to pi"

// Fragile: Exact equality on floats
assertEqual 3.14159 actual "should be pi"  // May fail due to rounding
```

## Extending the Framework

### Kleisli Composition (`>=>`)

The framework intentionally omits the Kleisli composition operator. While it completes the ROP abstraction, it rarely fits typical test patterns (arrange → act → assert).

If your tests involve reusable validation pipelines, you can add it:

```fsharp
let (>=>) f g x =
    match f x with
    | Pass y -> g y
    | Fail e -> Fail e
    | Skip r -> Skip r

// Example: composing reusable validation steps
let validatePositive x =
    if x > 0 then Pass x else fail "must be positive"

let validateEven x =
    if x % 2 = 0 then Pass x else fail "must be even"

let validatePositiveEven = validatePositive >=> validateEven

test "composed validation" (fun () ->
    match validatePositiveEven 4 with
    | Pass _ -> pass
    | Fail e -> Fail e
    | Skip r -> Skip r
)
```

For most test code, `combine` (parallel assertions) and `bind` (sequential, dependent assertions) cover the common cases.

## API Reference

### Core Functions

- `test: name -> (unit -> TestResult<unit>) -> Test`
- `testSkip: name -> reason -> Test`
- `suite: name -> Test list -> TestSuite`
- `suiteWith: name -> (unit -> 'env) -> ('env -> unit) -> (('env -> Test) list) -> TestSuite`

### Basic Assertions

- `assertEqual: 'a -> 'a -> string -> TestResult<unit>`
- `assertNotEqual: 'a -> 'a -> string -> TestResult<unit>`
- `assertTrue: bool -> string -> TestResult<unit>`
- `assertFalse: bool -> string -> TestResult<unit>`

### Option Assertions

- `assertSome: 'a option -> string -> TestResult<unit>`
- `assertNone: 'a option -> string -> TestResult<unit>`

### Result Assertions

- `assertOk: Result<'a,'e> -> string -> TestResult<unit>`
- `assertError: Result<'a,'e> -> string -> TestResult<unit>`

### Nil Checking

- `assertNil: 'a -> string -> TestResult<unit>`
- `assertNotNil: 'a -> string -> TestResult<unit>`

### Collection Assertions

- `assertEmpty: 'a -> string -> TestResult<unit>`
- `assertNotEmpty: 'a -> string -> TestResult<unit>`
- `assertLen: int -> 'a -> string -> TestResult<unit>`
- `assertContains: 'a -> 'b -> string -> TestResult<unit>`
- `assertNotContains: 'a -> 'b -> string -> TestResult<unit>`
- `assertSubset: 'a -> 'b -> string -> TestResult<unit>`
- `assertNotSubset: 'a -> 'b -> string -> TestResult<unit>`
- `assertElementsMatch: 'a -> 'b -> string -> TestResult<unit>`

### Numeric Assertions

- `assertGreater: 'a -> 'a -> string -> TestResult<unit>` (when 'a : comparison)
- `assertGreaterOrEqual: 'a -> 'a -> string -> TestResult<unit>` (when 'a : comparison)
- `assertLess: 'a -> 'a -> string -> TestResult<unit>` (when 'a : comparison)
- `assertLessOrEqual: 'a -> 'a -> string -> TestResult<unit>` (when 'a : comparison)
- `assertInDelta: 'a -> 'b -> float -> string -> TestResult<unit>` (numeric types)

### String Assertions

- `assertRegexp: string -> 'a -> string -> TestResult<unit>`
- `assertNotRegexp: string -> 'a -> string -> TestResult<unit>`

### Runners

- `parseTestArgs: string array -> TestSuite list -> int` (CLI entry point)
- `runAllSuites: TestSuite list -> SuiteOutcome list` (sequential)
- `runAllSuitesParallel: TestSuite list -> SuiteOutcome list` (parallel)
- `runSuiteByName: string -> TestSuite list -> SuiteOutcome list` (run one suite)
- `runSingleTest: string -> string -> TestSuite list -> SuiteOutcome list` (run one test)
- `runTestsMatching: string -> TestSuite list -> SuiteOutcome list` (pattern match)
- `printResults: SuiteOutcome list -> int` (returns exit code)

### Filtering

- `filterSuiteByName: string -> TestSuite list -> TestSuite list`
- `filterTestByName: string -> TestSuite -> TestSuite`
- `filterTests: (Test -> bool) -> TestSuite -> TestSuite`

### Railway Operators

- `bind: ('a -> TestResult<'b>) -> TestResult<'a> -> TestResult<'b>`
- `map: ('a -> 'b) -> TestResult<'a> -> TestResult<'b>`
- `combine: TestResult<unit> -> TestResult<unit> -> TestResult<unit>`

### Skip Functions

- `skipIf: bool -> string -> TestResult<unit>`
- `skipUnless: bool -> string -> TestResult<unit>`

## Why Railway-Oriented?

| Traditional Frameworks            | TestTracks                     |
| --------------------------------- | ------------------------------ |
| Control flow hidden in attributes | Tests are plain functions      |
| Exceptions for failures           | Failures are return values     |
| Framework controls execution      | You control execution          |
| Setup/teardown via reflection     | Explicit data flow             |
| Hard to compose                   | Compose with `bind`, `combine` |

**Traditional (NUnit/xUnit style):**

```fsharp
[<Test>]
let ``my test`` () =
    let result = setup()          // What if this throws?
    Assert.IsTrue(result.IsOk)    // Exception on failure
    Assert.AreEqual(42, value)    // Another exception?
    // Control flow: invisible, exception-based
```

**TestTracks — single assertion:**

```fsharp
test "my test" (fun () ->
    let result = compute()
    assertEqual 42 result "should be 42"
)
```

**TestTracks — multiple independent assertions (accumulates errors):**

```fsharp
test "validate order" (fun () ->
    assertTrue (order.Total > 0) "total positive"
    |> combine (assertTrue (order.Items.Length > 0) "has items")
    |> combine (assertSome order.CustomerId "has customer")
)
```

**TestTracks — dependent assertions (short-circuits on failure):**

```fsharp
test "dependent checks" (fun () -> test' {
    do! assertTrue (x > 0) "must be positive"    // Fails? Stops here.
    do! assertTrue (x < 100) "must be under 100"
    return! assertEqual 42 x "should be 42"
})
```

Keep it simple, keep it pure, keep it functional.

## Development

This project uses [Tusk](https://github.com/rliebz/tusk) as a task runner.

```bash
# Show available tasks
tusk
```

See `tusk.yml` for all available tasks and options.