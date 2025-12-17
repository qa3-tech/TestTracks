namespace TestTracks.Tests

open System
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
              dependentTests ]

        TestTracks.parseTestArgs args allSuites 
