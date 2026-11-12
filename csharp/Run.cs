namespace TestTracksCSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;


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
/// Command line entry point: <c>return Cli.Run(args, suites);</c>
/// </summary>
public static class Cli
{
  public static int Run(string[] args, IEnumerable<TestTracks.TestSuite> suites) =>
      TestTracks.parseTestArgs(args, ListModule.OfSeq(suites));
}

/// <summary>
/// Result formatting and printing. Delegates to the F# formatters so both share one output format.
/// </summary>
public static class Printer
{
  public static int Print(IReadOnlyList<SuiteResult> results) =>
      TestTracks.printResults(ToFSharp(results));

  public static string FormatDuration(double ms) => TestTracks.formatDuration(ms);

  public static string ToJUnitXml(IReadOnlyList<SuiteResult> results) =>
      TestTracks.summarizeJUnit(ToFSharp(results));

  public static void WriteJUnitXml(IReadOnlyList<SuiteResult> results, string filePath) =>
      System.IO.File.WriteAllText(filePath, ToJUnitXml(results));

  private static FSharpList<TestTracks.SuiteOutcome> ToFSharp(IReadOnlyList<SuiteResult> results) =>
      ListModule.OfSeq(results.Select(r => new TestTracks.SuiteOutcome(
          r.Name,
          ListModule.OfSeq(r.Results.Select(ToFSharpOutcome)),
          r.TotalDurationMs
      )));

  private static TestTracks.TestOutcome ToFSharpOutcome(TestOutcomeResult r) => r.Kind switch
  {
    TestOutcomeKind.Passed => TestTracks.TestOutcome.NewPassed(r.Name, r.DurationMs),
    TestOutcomeKind.Skipped => TestTracks.TestOutcome.NewSkipped(r.Name, r.SkipReason ?? ""),
    TestOutcomeKind.Failed => TestTracks.TestOutcome.NewFailed(
        r.Name,
        ListModule.OfSeq(r.Errors.Select(Outcomes.ToFSharpError)),
        r.DurationMs
    ),
    _ => TestTracks.TestOutcome.NewErrored(r.Name, r.Exception ?? new Exception("Unknown"), r.DurationMs)
  };
}
