module ResultTests
open Expecto

[<EntryPoint>]
let main args =
    let cliArgs = [ Allow_Duplicate_Names ]
    runTestsInAssemblyWithCLIArgs cliArgs args
