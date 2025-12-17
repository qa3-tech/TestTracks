namespace TestTracks.Tests

open System
open TestTracks
open Examples

module Program =
    [<EntryPoint>]
    let main args =
        let allSuites = [ basicTests; fileLoggingTests; railwayTests; databaseTests ]

        TestTracks.parseTestArgs args allSuites
