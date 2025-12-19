namespace TestTracksCSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;


public enum OutcomeKind { Passed, Failed, Skipped }

/// <summary>
/// C#-friendly test error
/// </summary>
public readonly record struct TestError(string Message, object? Expected = null, object? Actual = null);

/// <summary>
/// C#-friendly test result
/// </summary>
public readonly record struct Outcome(
    OutcomeKind Kind,
    IReadOnlyList<TestError> Errors,
    string? SkipReason
);

/// <summary>
/// Functions for working with Outcome
/// </summary>
public static class Outcomes
{
    public static Outcome Pass() => new(OutcomeKind.Passed, [], null);

    public static Outcome Fail(string message) => new(OutcomeKind.Failed, [new TestError(message)], null);

    public static Outcome Fail(string message, object expected, object actual) =>
        new(OutcomeKind.Failed, [new TestError(message, expected, actual)], null);

    public static Outcome Fail(IReadOnlyList<TestError> errors) => new(OutcomeKind.Failed, errors, null);

    public static Outcome Skip(string reason) => new(OutcomeKind.Skipped, [], reason);

    public static bool IsPassed(Outcome o) => o.Kind == OutcomeKind.Passed;
    public static bool IsFailed(Outcome o) => o.Kind == OutcomeKind.Failed;
    public static bool IsSkipped(Outcome o) => o.Kind == OutcomeKind.Skipped;

    public static Outcome And(Outcome a, Outcome b) => (a.Kind, b.Kind) switch
    {
        (OutcomeKind.Skipped, _) => a,
        (_, OutcomeKind.Skipped) => b,
        (OutcomeKind.Passed, OutcomeKind.Passed) => Pass(),
        (OutcomeKind.Failed, OutcomeKind.Passed) => a,
        (OutcomeKind.Passed, OutcomeKind.Failed) => b,
        (OutcomeKind.Failed, OutcomeKind.Failed) => Fail([.. a.Errors, .. b.Errors]),
        _ => a
    };

    public static Outcome All(params Outcome[] results) =>
        results.Length == 0 ? Pass() : results.Aggregate(And);

    internal static Outcome FromFSharp(TestTracks.TestResult<Unit> result) => result switch
    {
        TestTracks.TestResult<Unit>.Pass => Pass(),
        TestTracks.TestResult<Unit>.Fail f => Fail(
            f.Item.Select(e => new TestError(e.Message, e.Expected?.Value, e.Actual?.Value)).ToList()
        ),
        TestTracks.TestResult<Unit>.Skip s => Skip(s.Item),
        _ => Fail("Unknown result type")
    };
}

/// <summary>
/// Result of running a single test
/// </summary>
public readonly record struct TestOutcomeResult(
    TestOutcomeKind Kind,
    string Name,
    double DurationMs,
    IReadOnlyList<TestError> Errors,
    string? SkipReason,
    Exception? Exception
);

public enum TestOutcomeKind { Passed, Failed, Skipped, Errored }

/// <summary>
/// Functions for working with TestOutcomeResult
/// </summary>
public static class TestOutcomeResults
{
    internal static TestOutcomeResult FromFSharp(TestTracks.TestOutcome outcome) => outcome switch
    {
        TestTracks.TestOutcome.Passed p => new(TestOutcomeKind.Passed, p.name, p.durationMs, [], null, null),
        TestTracks.TestOutcome.Failed f => new(
            TestOutcomeKind.Failed,
            f.name,
            f.durationMs,
            f.errors.Select(e => new TestError(e.Message, e.Expected?.Value, e.Actual?.Value)).ToList(),
            null,
            null
        ),
        TestTracks.TestOutcome.Skipped s => new(TestOutcomeKind.Skipped, s.name, 0, [], s.reason, null),
        TestTracks.TestOutcome.Errored e => new(TestOutcomeKind.Errored, e.name, e.durationMs, [], null, e.exn),
        _ => new(TestOutcomeKind.Errored, "Unknown", 0, [], null, null)
    };
}

/// <summary>
/// Result of running a test suite
/// </summary>
public readonly record struct SuiteResult(
    string Name,
    IReadOnlyList<TestOutcomeResult> Results,
    double TotalDurationMs
);

/// <summary>
/// Functions for working with SuiteResult
/// </summary>
public static class SuiteResults
{
    public static int Passed(SuiteResult s) => s.Results.Count(r => r.Kind == TestOutcomeKind.Passed);
    public static int Failed(SuiteResult s) => s.Results.Count(r => r.Kind == TestOutcomeKind.Failed);
    public static int Skipped(SuiteResult s) => s.Results.Count(r => r.Kind == TestOutcomeKind.Skipped);
    public static int Errored(SuiteResult s) => s.Results.Count(r => r.Kind == TestOutcomeKind.Errored);
    public static bool AllPassed(SuiteResult s) => s.Results.All(r => r.Kind is TestOutcomeKind.Passed or TestOutcomeKind.Skipped);

    internal static SuiteResult FromFSharp(TestTracks.SuiteOutcome outcome) => new(
        outcome.SuiteName,
        outcome.Results.Select(TestOutcomeResults.FromFSharp).ToList(),
        outcome.TotalDurationMs
    );
}
