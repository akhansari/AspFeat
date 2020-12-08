module Program

open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext.Response

[<EntryPoint>]
let main _ =
    let configureEndpoints bld =
        get bld "/" (writeAsJson {| Id = 1; Product = "Cat Food" |})
    WebHost.run [ Endpoint.feat configureEndpoints ]
