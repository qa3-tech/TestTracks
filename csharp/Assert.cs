namespace TestTracksCSharp;

using System;


/// <summary>
/// Test assertions returning Outcome
/// </summary>
public static class Asserts
{
    // Equality
    public static Outcome Equal<T>(T expected, T actual, string message) =>
        Outcomes.FromFSharp(TestTracks.assertEqual(expected, actual, message));

    public static Outcome Equal<T>(T expected, T actual) =>
        Equal(expected, actual, "values should be equal");

    public static Outcome NotEqual<T>(T unexpected, T actual, string message) =>
        Outcomes.FromFSharp(TestTracks.assertNotEqual(unexpected, actual, message));

    public static Outcome NotEqual<T>(T unexpected, T actual) =>
        NotEqual(unexpected, actual, "values should not be equal");

    // Boolean
    public static Outcome True(bool condition, string message) =>
        Outcomes.FromFSharp(TestTracks.assertTrue(condition, message));

    public static Outcome True(bool condition) =>
        True(condition, "condition should be true");

    public static Outcome False(bool condition, string message) =>
        Outcomes.FromFSharp(TestTracks.assertFalse(condition, message));

    public static Outcome False(bool condition) =>
        False(condition, "condition should be false");

    // Nil
    public static Outcome Nil<T>(T value, string message) =>
        Outcomes.FromFSharp(TestTracks.assertNil(value, message));

    public static Outcome NotNil<T>(T value, string message) =>
        Outcomes.FromFSharp(TestTracks.assertNotNil(value, message));

    // Collections
    public static Outcome Empty<T>(T collection, string message) =>
        Outcomes.FromFSharp(TestTracks.assertEmpty(collection, message));

    public static Outcome NotEmpty<T>(T collection, string message) =>
        Outcomes.FromFSharp(TestTracks.assertNotEmpty(collection, message));

    public static Outcome Len<T>(int expected, T collection, string message) =>
        Outcomes.FromFSharp(TestTracks.assertLen(expected, collection, message));

    public static Outcome Contains<TElement, TCollection>(TElement element, TCollection collection, string message) =>
        Outcomes.FromFSharp(TestTracks.assertContains(element, collection, message));

    public static Outcome NotContains<TElement, TCollection>(TElement element, TCollection collection, string message) =>
        Outcomes.FromFSharp(TestTracks.assertNotContains(element, collection, message));

    public static Outcome Subset<TSubset, TCollection>(TSubset subset, TCollection collection, string message) =>
        Outcomes.FromFSharp(TestTracks.assertSubset(subset, collection, message));

    public static Outcome NotSubset<TSubset, TCollection>(TSubset subset, TCollection collection, string message) =>
        Outcomes.FromFSharp(TestTracks.assertNotSubset(subset, collection, message));

    public static Outcome ElementsMatch<TA, TB>(TA listA, TB listB, string message) =>
        Outcomes.FromFSharp(TestTracks.assertElementsMatch(listA, listB, message));

    // Numeric comparisons
    public static Outcome Greater<T>(T actual, T expected, string message) where T : IComparable<T> =>
        Outcomes.FromFSharp(TestTracks.assertGreater(actual, expected, message));

    public static Outcome GreaterOrEqual<T>(T actual, T expected, string message) where T : IComparable<T> =>
        Outcomes.FromFSharp(TestTracks.assertGreaterOrEqual(actual, expected, message));

    public static Outcome Less<T>(T actual, T expected, string message) where T : IComparable<T> =>
        Outcomes.FromFSharp(TestTracks.assertLess(actual, expected, message));

    public static Outcome LessOrEqual<T>(T actual, T expected, string message) where T : IComparable<T> =>
        Outcomes.FromFSharp(TestTracks.assertLessOrEqual(actual, expected, message));

    public static Outcome InDelta<TExpected, TActual>(TExpected expected, TActual actual, double delta, string message) =>
        Outcomes.FromFSharp(TestTracks.assertInDelta(expected, actual, delta, message));

    // Regex
    public static Outcome Regexp(string pattern, string str, string message) =>
        Outcomes.FromFSharp(TestTracks.assertRegexp(pattern, str, message));

    public static Outcome NotRegexp(string pattern, string str, string message) =>
        Outcomes.FromFSharp(TestTracks.assertNotRegexp(pattern, str, message));

    // Flow control
    public static Outcome Pass() => Outcomes.Pass();
    public static Outcome Fail(string message) => Outcomes.Fail(message);
    public static Outcome Skip(string reason) => Outcomes.Skip(reason);

    public static Outcome SkipIf(bool condition, string reason) =>
        condition ? Outcomes.Skip(reason) : Outcomes.Pass();

    public static Outcome SkipUnless(bool condition, string reason) =>
        condition ? Outcomes.Pass() : Outcomes.Skip(reason);
}

/// <summary>
/// Conditional test execution
/// </summary>
public static class Guards
{
    public static Outcome When(bool condition, string skipReason, Func<Outcome> test) =>
        condition ? test() : Outcomes.Skip(skipReason);

    public static Outcome Unless(bool condition, string skipReason, Func<Outcome> test) =>
        condition ? Outcomes.Skip(skipReason) : test();
}
