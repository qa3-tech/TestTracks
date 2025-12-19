namespace TestTracksCSharp.Tests;

using System;
using System.Collections.Generic;


public class Program
{
    public static IEnumerable<TestTracks.TestSuite> AllSuites =>
    [
        Examples.BasicTests,
        Examples.FileLoggingTests,
        Examples.ComposedTests,
        Examples.SkipTests,
        Examples.DataDrivenTests,
        Examples.ValidationTests,
        Examples.CollectionTests,
        Examples.NumericTests,
        Examples.StringTests,
        Examples.DatabaseTests
    ];

    public static int Main(string[] args)
    {
        Console.WriteLine("TestTracks C# Test Runner");
        Console.WriteLine("=========================\n");

        var results = Runner.All(AllSuites);
        return Printer.Print(results);
    }
}
