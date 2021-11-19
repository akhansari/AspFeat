[<AutoOpen>]
module Helpers

open System.Net.Http
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Builder
open AspFeat.Builder

let private withTestHost (builder: WebApplicationBuilder) =
    builder.WebHost.UseTestServer() |> ignore

let run feats =
    task {
        let app =
            (withTestHost, Feat.ignore) :: feats
            |> WebApp.create
        do! app.StartAsync()
        return app
    }

[<NoComparison>]
type RequestMethod =
    | Get of string
    | Delete of string
    | Put of (string * HttpContent)
    | Post of (string * HttpContent)
    | Patch of (string * HttpContent)

let request (host: WebApplication) method =
    task {
        use client = host.GetTestClient()
        return!
            match method with
            | Get uri -> client.GetAsync uri
            | Delete uri -> client.DeleteAsync uri
            | Post (uri, content) -> client.PostAsync(uri, content)
            | Put (uri, content) -> client.PutAsync(uri, content)
            | Patch (uri, content) -> client.PatchAsync(uri, content)
    }

let requestString host method =
    task {
        use! res = request host method
        return! res.Content.ReadAsStringAsync()
    }
