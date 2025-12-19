namespace TestTracksCSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;


/// <summary>
/// Test and suite construction functions
/// </summary>
public static class Tests
{
    public static TestTracks.Test Create(string name, Func<Outcome> run) =>
        TestTracks.test(name, FuncConvert.FromFunc(() =>
        {
            var o = run();
            return o.Kind switch
            {
                OutcomeKind.Passed => TestTracks.pass,
                OutcomeKind.Skipped => TestTracks.skip<Unit>(o.SkipReason ?? ""),
                OutcomeKind.Failed => TestTracks.fail<Unit>(o.Errors.FirstOrDefault().Message ?? "failed"),
                _ => TestTracks.fail<Unit>("unknown")
            };
        }));

    public static TestTracks.Test Skip(string name, string reason) =>
        TestTracks.testSkip(name, reason);
}

/// <summary>
/// Suite construction functions
/// </summary>
public static class Suites
{
    public static TestTracks.TestSuite Create(string name, IEnumerable<TestTracks.Test> tests) =>
        TestTracks.suite(name, ListModule.OfSeq(tests));

    public static TestTracks.TestSuite Create(string name, params TestTracks.Test[] tests) =>
        Create(name, tests.AsEnumerable());

    public static TestTracks.TestSuite CreateWith<TEnv>(
        string name,
        Func<TEnv> setup,
        Action<TEnv> teardown,
        params Func<TEnv, TestTracks.Test>[] tests)
    {
        var fsharpTests = tests
            .Select(t => FuncConvert.FromFunc<TEnv, TestTracks.Test>(env => t(env)))
            .ToList();

        return TestTracks.suiteWith(
            name,
            FuncConvert.FromFunc(setup),
            FuncConvert.FromAction(teardown),
            ListModule.OfSeq(fsharpTests)
        );
    }
}
