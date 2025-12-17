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
                printfn "[Teardown] Confirmed Test output deleted: %b" (not (File.Exists env.OutputFile)))
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

              test "skip expensive tests" (fun () ->
                  test' {
                      let runExpensive =
                          System.Environment.GetEnvironmentVariable("RUN_EXPENSIVE_TESTS") <> null

                      do! skipUnless runExpensive "set RUN_EXPENSIVE_TESTS=1 to run"
                      return! assertTrue true "expensive test logic here"
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

    let failingTests =
        suite
            "Failing Tests (Expected)"
            [ test "single assertion failure" (fun () -> assertEqual 42 99 "should be 42")

              test "multiple failures with combine" (fun () ->
                  assertTrue false "first check"
                  |> combine (assertEqual "expected" "actual" "second check")
                  |> combine (assertSome None "third check"))

              test "failure in test' short-circuits" (fun () ->
                  test' {
                      do! assertTrue false "fails here"
                      return! assertTrue true "never reaches this"
                  }) ]

    // Dependent assertions with test' (short-circuits)
    let dependentTests =
        suite
            "Dependent Tests"
            [ test "all pass when valid" (fun () ->
                  test' {
                      let x = 42
                      do! assertTrue (x > 0) "must be positive"
                      do! assertTrue (x < 100) "must be under 100"
                      return! assertEqual 42 x "value matches"
                  })

              test "short-circuits on failure" (fun () ->
                  let mutable secondRun = false

                  let result =
                      test' {
                          do! assertTrue false "this fails"
                          secondRun <- true // should never run
                          return! assertTrue true "unreachable"
                      }

                  match result with
                  | Fail _ -> assertFalse secondRun "second assertion should not run"
                  | _ -> fail "expected failure") ]


    // Nil checking tests
    let nilTests =
        suite
            "Nil Checking Tests"
            [ test "assertNil passes for null" (fun () -> assertNil null "should be null")

              test "assertNotNil passes for non-null" (fun () -> assertNotNil "hello" "should not be null")

              test "assertNotNil passes for zero" (fun () -> assertNotNil 0 "zero is not null")

              test "assertNil fails for non-null" (fun () ->
                  match assertNil "not null" "test" with
                  | Fail _ -> pass
                  | _ -> fail "expected failure") ]

    // Collection tests
    let collectionTests =
        suite
            "Collection Tests"
            [ test "assertEmpty passes for empty list" (fun () -> assertEmpty [] "should be empty")

              test "assertEmpty passes for empty array" (fun () -> assertEmpty [||] "should be empty")

              test "assertEmpty passes for empty string" (fun () -> assertEmpty "" "should be empty")

              test "assertEmpty passes for null" (fun () -> assertEmpty null "null is empty")

              test "assertNotEmpty passes for non-empty list" (fun () -> assertNotEmpty [ 1; 2; 3 ] "has elements")

              test "assertNotEmpty passes for non-empty string" (fun () -> assertNotEmpty "hello" "has characters")

              test "assertLen checks list length" (fun () -> assertLen 3 [ 1; 2; 3 ] "list has 3 elements")

              test "assertLen checks string length" (fun () -> assertLen 5 "hello" "string has 5 chars")

              test "assertLen checks array length" (fun () -> assertLen 2 [| 'a'; 'b' |] "array has 2 elements")

              test "assertContains finds element in list" (fun () -> assertContains 2 [ 1; 2; 3 ] "2 is in list")

              test "assertContains finds substring" (fun () -> assertContains "world" "hello world" "substring exists")

              test "assertNotContains passes when not found" (fun () -> assertNotContains 5 [ 1; 2; 3 ] "5 not in list")

              test "assertNotContains passes for missing substring" (fun () ->
                  assertNotContains "foo" "hello world" "foo not in string")

              test "assertSubset passes when all elements present" (fun () ->
                  assertSubset [ 1; 2 ] [ 1; 2; 3; 4 ] "subset found")

              test "assertSubset handles duplicates" (fun () ->
                  assertSubset [ 1; 1 ] [ 1; 2; 3 ] "duplicates in subset ok")

              test "assertNotSubset passes when missing element" (fun () ->
                  assertNotSubset [ 1; 5 ] [ 1; 2; 3 ] "5 not in collection")

              test "assertElementsMatch ignores order" (fun () ->
                  assertElementsMatch [ 3; 1; 2 ] [ 1; 2; 3 ] "same elements")

              test "assertElementsMatch handles duplicates" (fun () ->
                  assertElementsMatch [ 1; 2; 2; 3 ] [ 2; 3; 1; 2 ] "counts match")

              test "assertElementsMatch fails on different counts" (fun () ->
                  match assertElementsMatch [ 1; 1; 2 ] [ 1; 2; 2 ] "test" with
                  | Fail _ -> pass
                  | _ -> fail "expected failure") ]


    // Numeric comparison tests
    let numericTests =
        suite
            "Numeric Comparison Tests"
            [ test "assertGreater passes when greater" (fun () -> assertGreater 10 5 "10 > 5")

              test "assertGreater works with floats" (fun () -> assertGreater 3.14 2.0 "pi > 2")

              test "assertGreaterOrEqual passes when equal" (fun () -> assertGreaterOrEqual 5 5 "5 >= 5")

              test "assertGreaterOrEqual passes when greater" (fun () -> assertGreaterOrEqual 10 5 "10 >= 5")

              test "assertLess passes when less" (fun () -> assertLess 5 10 "5 < 10")

              test "assertLess works with negatives" (fun () -> assertLess -10 -5 "-10 < -5")

              test "assertLessOrEqual passes when equal" (fun () -> assertLessOrEqual 5 5 "5 <= 5")

              test "assertLessOrEqual passes when less" (fun () -> assertLessOrEqual 3 7 "3 <= 7")

              test "assertInDelta with floats" (fun () -> assertInDelta 1.0 1.05 0.1 "within 0.1")

              test "assertInDelta with ints" (fun () -> assertInDelta 10 12 3.0 "int within delta")

              test "assertInDelta with float32" (fun () -> assertInDelta 1.0f 1.05f 0.1 "float32 within delta")

              test "assertInDelta with decimals" (fun () -> assertInDelta 1.0m 1.05m 0.1 "decimal within delta")

              test "assertInDelta mixed int and float" (fun () -> assertInDelta 10 10.5 1.0 "mixed types")

              test "assertInDelta handles negative differences" (fun () ->
                  assertInDelta 1.0 0.95 0.1 "negative diff ok")

              test "assertInDelta exact match" (fun () -> assertInDelta 5.0 5.0 0.01 "exact match")

              test "assertInDelta fails outside tolerance" (fun () ->
                  match assertInDelta 1.0 2.0 0.5 "test" with
                  | Fail _ -> pass
                  | _ -> fail "expected failure") ]


    // String and regex tests
    let stringTests =
        suite
            "String and Regex Tests"
            [ test "assertRegexp matches simple pattern" (fun () ->
                  assertRegexp "^hello" "hello world" "starts with hello")

              test "assertRegexp matches digit pattern" (fun () -> assertRegexp @"\d+" "abc123def" "contains digits")

              test "assertRegexp matches email pattern" (fun () ->
                  assertRegexp @"\w+@\w+\.\w+" "test@example.com" "valid email")

              test "assertNotRegexp passes when no match" (fun () ->
                  assertNotRegexp @"^\d+" "hello123" "doesn't start with digit")

              test "assertNotRegexp fails when matches" (fun () ->
                  match assertNotRegexp "world" "hello world" "test" with
                  | Fail _ -> pass
                  | _ -> fail "expected failure") ]

    // Combined tests showing composition
    let compositionTests =
        suite
            "Composition Tests"
            [ test "multiple collection checks with combine" (fun () ->
                  let list = [ 1; 2; 3; 4; 5 ]

                  assertNotEmpty list "not empty"
                  |> combine (assertLen 5 list "length 5")
                  |> combine (assertContains 3 list "contains 3"))

              test "numeric range validation" (fun () ->
                  let value = 50

                  assertGreater value 0 "positive"
                  |> combine (assertLess value 100 "under 100")
                  |> combine (assertGreaterOrEqual value 50 "at least 50"))

              test "string validation pipeline" (fun () ->
                  test' {
                      let email = "user@example.com"
                      do! assertNotEmpty email "not empty"
                      do! assertRegexp "@" email "contains @"
                      return! assertRegexp @"\.\w+$" email "has domain extension"
                  })

              test "collection subset and match together" (fun () ->
                  let main = [ 1; 2; 3; 4; 5 ]
                  let sub = [ 2; 3 ]
                  let copy = [ 5; 4; 3; 2; 1 ]

                  assertSubset sub main "subset exists"
                  |> combine (assertElementsMatch main copy "same elements")) ]

    // Edge case tests
    let edgeCaseTests =
        suite
            "Edge Case Tests"
            [ test "assertElementsMatch with empty lists" (fun () -> assertElementsMatch [] [] "both empty")

              test "assertElementsMatch single element" (fun () -> assertElementsMatch [ 1 ] [ 1 ] "single match")

              test "assertLen with zero" (fun () -> assertLen 0 [] "zero length")

              test "assertSubset empty subset" (fun () -> assertSubset [] [ 1; 2; 3 ] "empty subset always true")

              test "assertInDelta with zero delta" (fun () -> assertInDelta 1.0 1.0 0.0 "exact match only")

              test "assertContains with different types" (fun () ->
                  assertContains "123" "test123abc" "number as substring")

              test "assertRegexp empty pattern matches" (fun () -> assertRegexp "" "anything" "empty regex matches") ]

    // Data-driven numeric tests
    let numericDataDrivenTests =
        let comparisons =
            [ (10, 5, "assertGreater")
              (5, 5, "assertGreaterOrEqual")
              (5, 10, "assertLess")
              (5, 5, "assertLessOrEqual") ]

        suite
            "Data-Driven Numeric Tests"
            [ for (a, b, name) in comparisons do
                  test (sprintf "%s: %d vs %d" name a b) (fun () ->
                      match name with
                      | "assertGreater" -> assertGreater a b "test"
                      | "assertGreaterOrEqual" -> assertGreaterOrEqual a b "test"
                      | "assertLess" -> assertLess a b "test"
                      | "assertLessOrEqual" -> assertLessOrEqual a b "test"
                      | _ -> fail "unknown comparison") ]

    // Regex pattern tests
    let regexPatternTests =
        let patterns =
            [ (@"^\d{3}-\d{4}$", "123-4567", true, "phone number")
              (@"^[A-Z]\w+$", "Hello", true, "capitalized word")
              (@"\d+", "abc", false, "no digits")
              (@"^test", "testing", true, "starts with test")
              (@"test$", "contest", true, "ends with test") ]

        suite
            "Regex Pattern Tests"
            [ for (pattern, input, shouldMatch, desc) in patterns do
                  test (sprintf "%s: '%s'" desc input) (fun () ->
                      match shouldMatch with
                      | true -> assertRegexp pattern input desc
                      | false -> assertNotRegexp pattern input desc) ]
