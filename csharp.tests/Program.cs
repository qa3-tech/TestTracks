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
        Examples.DatabaseTests,
        Examples.JUnit
  ];

  public static int Main(string[] args)
  {
    Console.WriteLine("TestTracks C# Test Runner");
    Console.WriteLine("=========================\n");

    return TestTracks.parseTestArgs(
        args,
        Microsoft.FSharp.Collections.ListModule.OfSeq(AllSuites)
    );
  }
}
