namespace TestTracks.Tests

open TestTracks
open System.IO

module Examples =

    // Test environment with output file
    type TestEnvironment =
        { OutputFile: string
          StartTime: System.DateTime
          TestCounter: int }

    let writeLog (env: TestEnvironment) message =
        File.AppendAllText(env.OutputFile, sprintf "[%s] %s\n" (System.DateTime.Now.ToString "HH:mm:ss") message)

    // Basic tests without setup
    let basicTests =
        suite
            "Basic Math Tests"
            [ test "addition works" (fun () -> assertEqual 4 (2 + 2) "two plus two equals four")

              test "multiplication works" (fun () -> assertEqual 42 (6 * 7) "six times seven equals forty-two")

              testSkip "division by zero" "not implemented yet" ]

    // Tests with shared environment and file logging
    let fileLoggingTests =
        suiteWith
            "File Logging Tests"
            // Buildup: Create environment with output file
            (fun () ->
                let outputFile =
                    Path.Combine(Path.GetTempPath(), sprintf "test-output-%d.log" System.DateTime.Now.Ticks)

                File.WriteAllText(outputFile, "=== Test Session Started ===\n")
                printfn "[Buildup] Created output file: %s" outputFile

                { OutputFile = outputFile
                  StartTime = System.DateTime.Now
                  TestCounter = 0 })
            // Teardown: Clean up file
            (fun env ->
                writeLog env "Test session completed"
                printfn "[Teardown] Test output written to: %s" env.OutputFile
                printfn "[Teardown] Confirmed Test output exists: %b" (File.Exists env.OutputFile)
                File.Delete env.OutputFile
                printfn "[Teardown] Confirmed Test output deleted: %b" (not (File.Exists env.OutputFile))
            )
            // tests to run
            [
              // Each test receives the environment and can write to the file
              fun env ->
                  test "can write to log file" (fun () ->
                      writeLog env "Test 1 executing"
                      assertTrue (File.Exists env.OutputFile) "output file should exist")

              fun env ->
                  test "log file has content" (fun () ->
                      writeLog env "Test 2 executing"
                      let content = File.ReadAllText env.OutputFile
                      assertTrue (content.Contains "Test Session Started") "should have header")

              fun env ->
                  test "environment is shared" (fun () ->
                      writeLog env "Test 3 executing"
                      let lines = File.ReadAllLines env.OutputFile
                      assertTrue (lines.Length > 2) "should have multiple log entries") ]

    // Railway-oriented programming examples
    let railwayTests =
        suite
            "Railway-Oriented Tests"
            [

              test "successful pipeline" (fun () ->
                  let result = Ok 10 |> Result.map ((*) 2) |> Result.map ((+) 5)
                  assertEqual result (Ok 25) "should compute correctly")

              test "failed pipeline short-circuits" (fun () ->
                  test' {
                      let pipeline =
                          Ok 10
                          |> Result.bind (fun x -> if x > 5 then Error "too large" else Ok x)
                          |> Result.map ((*) 2)

                      return! assertError pipeline "should fail at validation"
                  })

              test "skip based on condition" (fun () ->
                  test' {
                      let shouldSkip = false
                      do! skipIf shouldSkip "condition not met"
                      return! assertTrue true "test continues if not skipped"
                  })

              test "composed assertions" (fun () ->
                  let x = 42

                  assertTrue (x > 0) "should be positive"
                  |> combine (assertEqual 42 x "should equal 42")
                  |> combine (assertNotEqual 0 x "should not be zero")) ]

    // Database example with complex environment
    type Database =
        { Connection: string
          IsOpen: bool
          Data: Map<string, string> }

    let databaseTests =
        suiteWith
            "Database Tests"
            (fun () ->
                printfn "[Buildup] Opening test database"

                { Connection = "test-db"
                  IsOpen = true
                  Data = Map.empty })
            (fun db -> printfn "[Teardown] Closing database: %s" db.Connection)
            [ fun db -> test "database is open" (fun () -> assertTrue db.IsOpen "connection should be open")

              fun db ->
                  test "can insert data" (fun () ->
                      let db' =
                          { db with
                              Data = db.Data.Add("key1", "value1") }

                      assertTrue (db'.Data.ContainsKey "key1") "should contain inserted key")

              fun db ->
                  test "has connection string" (fun () ->
                      assertEqual "test-db" db.Connection "should have correct connection") ]

    let skipTests =
        suite
            "Skip Tests"
            [ testSkip "not implemented" "waiting for feature X"

              test "skip on windows" (fun () ->
                  test' {
                      let isWindows =
                          System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                              System.Runtime.InteropServices.OSPlatform.Windows
                          )

                      do! skipIf isWindows "Linux/macOS only"
                      return! assertTrue true "runs on non-Windows"
                  })

              test "skip in CI" (fun () ->
                  test' {
                      let isCI = System.Environment.GetEnvironmentVariable("CI") <> null
                      do! skipIf isCI "too slow for CI"
                      return! assertTrue true "runs locally"
                  })

              test "require env var" (fun () ->
                  test' {
                      let apiKey = System.Environment.GetEnvironmentVariable("API_KEY")
                      do! skipUnless (apiKey <> null) "API_KEY not set"
                      return! assertTrue (apiKey.Length > 0) "API_KEY has value"
                  }) ]

    let dataDrivenTests =
        let doublingCases = [ (2, 4); (5, 10); (10, 20); (0, 0); (-5, -10) ]

        suite
            "Data-Driven Tests"
            [ for (input, expected) in doublingCases do
                  test (sprintf "%d * 2 = %d" input expected) (fun () ->
                      assertEqual expected (input * 2) "should double") ]

    let additionTests =
        let cases = [ (1, 1, 2); (0, 0, 0); (-1, 1, 0); (100, 200, 300) ]

        suite
            "Addition Tests"
            [ for (a, b, expected) in cases do
                  test (sprintf "%d + %d = %d" a b expected) (fun () -> assertEqual expected (a + b) "should add") ]

    // Validation example with combine (accumulates all errors)
    type Order =
        { Total: decimal
          Items: string list
          CustomerId: int option }

    let validationTests =
        let validOrder =
            { Total = 99.99m
              Items = [ "Widget" ]
              CustomerId = Some 123 }

        let invalidOrder =
            { Total = 0m
              Items = []
              CustomerId = None }

        suite
            "Validation Tests"
            [ test "valid order passes all checks" (fun () ->
                  assertTrue (validOrder.Total > 0m) "total positive"
                  |> combine (assertTrue (validOrder.Items.Length > 0) "has items")
                  |> combine (assertSome validOrder.CustomerId "has customer"))

              test "invalid order fails multiple checks" (fun () ->
                  // This test expects failures - we invert to prove combine accumulates
                  let result =
                      assertTrue (invalidOrder.Total > 0m) "total positive"
                      |> combine (assertTrue (invalidOrder.Items.Length > 0) "has items")
                      |> combine (assertSome invalidOrder.CustomerId "has customer")

                  match result with
                  | Fail errors -> assertEqual 3 errors.Length "should have 3 errors"
                  | _ -> fail "expected failures") ]

    let failingTests = suite "Failing Tests (Expected)" [
        test "single assertion failure" (fun () ->
            assertEqual 42 99 "should be 42"
        )
    
        test "multiple failures with combine" (fun () ->
            assertTrue false "first check"
            |> combine (assertEqual "expected" "actual" "second check")
            |> combine (assertSome None "third check")
        )
    
        test "failure in test' short-circuits" (fun () -> test' {
            do! assertTrue false "fails here"
            return! assertTrue true "never reaches this"
        })
    ]
                  
    // Dependent assertions with test' (short-circuits)
    let dependentTests = suite "Dependent Tests" [
        test "all pass when valid" (fun () -> test' {
            let x = 42
            do! assertTrue (x > 0) "must be positive"
            do! assertTrue (x < 100) "must be under 100"
            return! assertEqual 42 x "value matches"
        })
    
        test "short-circuits on failure" (fun () ->
            let mutable secondRun = false
            let result = test' {
                do! assertTrue false "this fails"
                secondRun <- true  // should never run
                return! assertTrue true "unreachable"
            }
            match result with
            | Fail _ -> assertFalse secondRun "second assertion should not run"
            | _ -> fail "expected failure"
        )
    ]