open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Logging
open AspFeat
open AspFeat.Endpoint
open AspFeat.HttpContext
open AspFeat.Builder

type Hello = { Hello: string }

let configureEndpoints bld =
    endpoints bld {
        get "/hello" (writeAsJson { Hello = "AspFeat" })
    }

[<EntryPoint>]
let main args =
    [ (fun (b: WebApplicationBuilder) -> b.Logging.ClearProviders() |> ignore), Feat.ignore
      Endpoint.feat configureEndpoints ]
    |> WebApp.run args
