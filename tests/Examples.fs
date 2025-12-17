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
            // Optionally delete: File.Delete(env.OutputFile)
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
