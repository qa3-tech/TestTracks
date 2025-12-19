namespace TestTracksCSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;


/// <summary>
/// Test execution functions
/// </summary>
public static class Runner
{
    public static SuiteResult Suite(TestTracks.TestSuite suite) =>
        SuiteResults.FromFSharp(TestTracks.runSuite(suite));

    public static IReadOnlyList<SuiteResult> All(IEnumerable<TestTracks.TestSuite> suites) =>
        TestTracks.runAllSuites(ListModule.OfSeq(suites))
          .Select(SuiteResults.FromFSharp)
          .ToList();

    public static IReadOnlyList<SuiteResult> All(params TestTracks.TestSuite[] suites) =>
        All(suites.AsEnumerable());

    public static IReadOnlyList<SuiteResult> Parallel(IEnumerable<TestTracks.TestSuite> suites) =>
        TestTracks.runAllSuitesParallel(ListModule.OfSeq(suites))
          .Select(SuiteResults.FromFSharp)
          .ToList();

    public static IReadOnlyList<SuiteResult> Parallel(params TestTracks.TestSuite[] suites) =>
        Parallel(suites.AsEnumerable());
}

/// <summary>
/// Result formatting and printing functions
/// </summary>
public static class Printer
{
    public static int Print(IReadOnlyList<SuiteResult> results)
    {
        foreach (var suite in results)
        {
            Console.WriteLine($"\n=== {suite.Name} ({FormatDuration(suite.TotalDurationMs)}) ===");
            foreach (var test in suite.Results)
            {
                Console.WriteLine(FormatTest(test));
            }
        }

        var total = results.Sum(s => s.Results.Count);
        var passed = results.Sum(SuiteResults.Passed);
        var failed = results.Sum(SuiteResults.Failed);
        var skipped = results.Sum(SuiteResults.Skipped);
        var errored = results.Sum(SuiteResults.Errored);
        var totalTime = results.Sum(s => s.TotalDurationMs);

        Console.WriteLine($"\n{passed}/{total} passed, {failed} failed, {skipped} skipped, {errored} errored (Total: {FormatDuration(totalTime)})");

        return failed + errored == 0 ? 0 : 1;
    }

    public static string FormatDuration(double ms) => ms switch
    {
        < 1 => $"{ms:F2}ms",
        < 1000 => $"{ms:F0}ms",
        _ => $"{ms / 1000:F2}s"
    };

    public static string FormatTest(TestOutcomeResult t) => t.Kind switch
    {
        TestOutcomeKind.Passed => $"✓ {t.Name} ({FormatDuration(t.DurationMs)})",
        TestOutcomeKind.Skipped => $"⊘ {t.Name} (skipped: {t.SkipReason})",
        TestOutcomeKind.Errored => $"✗ {t.Name} ({FormatDuration(t.DurationMs)})\n  Exception: {t.Exception?.Message}",
        TestOutcomeKind.Failed => $"✗ {t.Name} ({FormatDuration(t.DurationMs)})\n{FormatErrors(t.Errors)}",
        _ => $"? {t.Name}"
    };

    public static string FormatErrors(IReadOnlyList<TestError> errors) =>
        string.Join("\n", errors.Select(e =>
            e.Expected is not null && e.Actual is not null
                ? $"  ✗ {e.Message}\n    Expected: {e.Expected}\n    Actual:   {e.Actual}"
                : $"  ✗ {e.Message}"
        ));
}
