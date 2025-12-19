namespace TestTracksCSharp.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using TestTracksCSharp;

public static class Examples
{
    // Test environment for file logging tests
    public record TestEnvironment(string OutputFile, DateTime StartTime);

    static void WriteLog(TestEnvironment env, string message) =>
        File.AppendAllText(env.OutputFile, $"[{DateTime.Now:HH:mm:ss}] {message}\n");

    // Basic tests
    public static TestTracks.TestSuite BasicTests => Suites.Create("Basic Math Tests (C#)",
        Tests.Create("addition works", () => Asserts.Equal(4, 2 + 2, "two plus two equals four")),
        Tests.Create("multiplication works", () => Asserts.Equal(42, 6 * 7, "six times seven equals forty-two")),
        Tests.Skip("division by zero", "not implemented yet")
    );

    // Tests with setup/teardown
    public static TestTracks.TestSuite FileLoggingTests => Suites.CreateWith(
        "File Logging Tests (C#)",
        setup: () =>
        {
            var outputFile = Path.Combine(Path.GetTempPath(), $"test-output-{DateTime.Now.Ticks}.log");
            File.WriteAllText(outputFile, "=== Test Session Started ===\n");
            Console.WriteLine($"[Setup] Created: {outputFile}");
            return new TestEnvironment(outputFile, DateTime.Now);
        },
        teardown: env =>
        {
            WriteLog(env, "Test session completed");
            File.Delete(env.OutputFile);
            Console.WriteLine("[Teardown] Cleaned up");
        },
        env => Tests.Create("can write to log file", () =>
        {
            WriteLog(env, "Test 1 executing");
            return Asserts.True(File.Exists(env.OutputFile), "output file should exist");
        }),
        env => Tests.Create("log file has content", () =>
        {
            WriteLog(env, "Test 2 executing");
            var content = File.ReadAllText(env.OutputFile);
            return Asserts.True(content.Contains("Test Session Started"), "should have header");
        }),
        env => Tests.Create("environment is shared", () =>
        {
            WriteLog(env, "Test 3 executing");
            var lines = File.ReadAllLines(env.OutputFile);
            return Asserts.Greater(lines.Length, 2, "should have multiple log entries");
        })
    );

    // Composed assertions using Outcomes.All
    public static TestTracks.TestSuite ComposedTests => Suites.Create("Composed Tests (C#)",
        Tests.Create("multiple checks with All", () =>
        {
            var x = 42;
            return Outcomes.All(
                Asserts.True(x > 0, "should be positive"),
                Asserts.Equal(42, x, "should equal 42"),
                Asserts.NotEqual(0, x, "should not be zero")
            );
        }),
        Tests.Create("chained with And", () =>
        {
            var x = 42;
            return Outcomes.And(
                Asserts.True(x > 0, "positive"),
                Outcomes.And(
                    Asserts.Equal(42, x, "is 42"),
                    Asserts.NotEqual(0, x, "not zero")
                )
            );
        })
    );

    // Skip tests using Guards
    public static TestTracks.TestSuite SkipTests => Suites.Create("Skip Tests (C#)",
        Tests.Skip("not implemented", "waiting for feature X"),

        Tests.Create("skip on windows (When)", () =>
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return Guards.When(!isWindows, "Linux/macOS only", () =>
                Asserts.True(true, "runs on non-Windows")
            );
        }),

        Tests.Create("skip on windows (Unless)", () =>
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return Guards.Unless(isWindows, "Windows only", () =>
                Asserts.True(true, "runs on Windows")
            );
        }),

        Tests.Create("require env var", () =>
        {
            var apiKey = Environment.GetEnvironmentVariable("API_KEY");
            return Guards.When(apiKey != null, "API_KEY not set", () =>
                Asserts.True(apiKey!.Length > 0, "API_KEY has value")
            );
        }),

        Tests.Create("skip expensive tests", () =>
        {
            var runExpensive = Environment.GetEnvironmentVariable("RUN_EXPENSIVE_TESTS") != null;
            return Guards.When(runExpensive, "set RUN_EXPENSIVE_TESTS=1", () =>
                Asserts.True(true, "expensive test logic here")
            );
        })
    );

    // Data-driven tests
    public static TestTracks.TestSuite DataDrivenTests
    {
        get
        {
            var cases = new[] { (2, 4), (5, 10), (10, 20), (0, 0), (-5, -10) };
            var tests = cases.Select(c =>
                Tests.Create($"{c.Item1} * 2 = {c.Item2}", () =>
                    Asserts.Equal(c.Item2, c.Item1 * 2, "should double"))
            );
            return Suites.Create("Data-Driven Tests (C#)", tests);
        }
    }

    // Validation with error accumulation
    public record Order(decimal Total, List<string> Items, int? CustomerId);

    public static TestTracks.TestSuite ValidationTests
    {
        get
        {
            var validOrder = new Order(99.99m, ["Widget"], 123);
            var invalidOrder = new Order(0m, [], null);

            return Suites.Create("Validation Tests (C#)",
                Tests.Create("valid order passes all checks", () =>
                    Outcomes.All(
                        Asserts.True(validOrder.Total > 0m, "total positive"),
                        Asserts.True(validOrder.Items.Count > 0, "has items"),
                        Asserts.True(validOrder.CustomerId.HasValue, "has customer")
                    )
                ),
                Tests.Create("invalid order accumulates errors", () =>
                {
                    var result = Outcomes.All(
                        Asserts.True(invalidOrder.Total > 0m, "total positive"),
                        Asserts.True(invalidOrder.Items.Count > 0, "has items"),
                        Asserts.True(invalidOrder.CustomerId.HasValue, "has customer")
                    );
                    return Outcomes.IsFailed(result)
                        ? Asserts.Equal(3, result.Errors.Count, "should have 3 errors")
                        : Asserts.Fail("expected failures");
                })
            );
        }
    }

    // Collection tests
    public static TestTracks.TestSuite CollectionTests => Suites.Create("Collection Tests (C#)",
        Tests.Create("empty checks", () =>
            Outcomes.All(
                Asserts.Empty(Array.Empty<int>(), "empty array"),
                Asserts.Empty("", "empty string"),
                Asserts.NotEmpty(new[] { 1, 2, 3 }, "has elements"),
                Asserts.NotEmpty("hello", "has chars")
            )
        ),
        Tests.Create("length and contains", () =>
            Outcomes.All(
                Asserts.Len(3, new[] { 1, 2, 3 }, "array length"),
                Asserts.Len(5, "hello", "string length"),
                Asserts.Contains(2, new[] { 1, 2, 3 }, "element in array"),
                Asserts.Contains("world", "hello world", "substring")
            )
        ),
        Tests.Create("subset and elements match", () =>
            Outcomes.All(
                Asserts.Subset(new[] { 1, 2 }, new[] { 1, 2, 3, 4 }, "subset found"),
                Asserts.ElementsMatch(new[] { 3, 1, 2 }, new[] { 1, 2, 3 }, "same elements, different order")
            )
        )
    );

    // Numeric tests
    public static TestTracks.TestSuite NumericTests => Suites.Create("Numeric Tests (C#)",
        Tests.Create("comparisons", () =>
            Outcomes.All(
                Asserts.Greater(10, 5, "10 > 5"),
                Asserts.GreaterOrEqual(5, 5, "5 >= 5"),
                Asserts.Less(5, 10, "5 < 10"),
                Asserts.LessOrEqual(5, 5, "5 <= 5")
            )
        ),
        Tests.Create("delta checks", () =>
            Outcomes.All(
                Asserts.InDelta(1.0, 1.05, 0.1, "within delta"),
                Asserts.InDelta(10, 12, 3.0, "int within delta")
            )
        ),
        Tests.Create("range validation", () =>
        {
            var value = 50;
            return Outcomes.All(
                Asserts.Greater(value, 0, "positive"),
                Asserts.Less(value, 100, "under 100"),
                Asserts.GreaterOrEqual(value, 50, "at least 50")
            );
        })
    );

    // String/regex tests
    public static TestTracks.TestSuite StringTests => Suites.Create("String Tests (C#)",
        Tests.Create("regex matches", () =>
            Outcomes.All(
                Asserts.Regexp("^hello", "hello world", "starts with hello"),
                Asserts.Regexp(@"\d+", "abc123def", "contains digits"),
                Asserts.NotRegexp(@"^\d+", "hello123", "doesn't start with digit")
            )
        ),
        Tests.Create("regex failure check", () =>
        {
            var result = Asserts.NotRegexp("world", "hello world", "test");
            return Outcomes.IsFailed(result) ? Asserts.Pass() : Asserts.Fail("expected failure");
        })
    );

    // Database example
    public record Database(string Connection, bool IsOpen, Dictionary<string, string> Data);

    public static TestTracks.TestSuite DatabaseTests => Suites.CreateWith(
        "Database Tests (C#)",
        setup: () =>
        {
            Console.WriteLine("[Setup] Opening test database");
            return new Database("test-db", true, new Dictionary<string, string>());
        },
        teardown: db => Console.WriteLine($"[Teardown] Closing: {db.Connection}"),
        db => Tests.Create("database is open", () => Asserts.True(db.IsOpen, "should be open")),
        db => Tests.Create("can query connection", () => Asserts.Equal("test-db", db.Connection, "connection string"))
    );
}
