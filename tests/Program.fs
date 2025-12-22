namespace TestTracks.Tests

open TestTracks
open Examples

module Program =
    [<EntryPoint>]
    let main args =
        let allSuites =
            [ basicTests
              fileLoggingTests
              railwayTests
              databaseTests
              skipTests
              dataDrivenTests
              additionTests
              validationTests
              failingTests
              dependentTests
              nilTests
              numericTests
              stringTests
              compositionTests
              edgeCaseTests
              regexPatternTests
              junitTests ]

        parseTestArgs args allSuites
