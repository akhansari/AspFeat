module Program

open AspFeat.Endpoint.Builder
open AspFeat.Endpoint.Response
open AspFeat.Builder

[<EntryPoint>]
let main _ =
    let configureEndpoints bld =
        get bld "/" (writeAsJson {| Id = 1; Product = "Cat Food" |})
    [ Endpoint.featWith configureEndpoints ]
    |> WebHost.run
