[<AutoOpen>]
module Helpers

open System.Net.Http
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Hosting
open AspFeat.Builder

let private withTestHost (builder: IWebHostBuilder) =
    builder.UseTestServer()

let run feats =
    let builder = WebHost.create withTestHost feats
    builder.StartAsync () |> Async.AwaitTask

type RequestMethod =
    | Get of string
    | Delete of string
    | Put of (string * HttpContent)
    | Post of (string * HttpContent)
    | Patch of (string * HttpContent)

let request (host: IHost) method = 
    task {
        use client = host.GetTestClient()
        return!
            match method with
            | Get uri -> client.GetAsync uri
            | Delete uri -> client.DeleteAsync uri
            | Post (uri, content) -> client.PostAsync (uri, content)
            | Put (uri, content) -> client.PutAsync (uri, content)
            | Patch (uri, content) -> client.PatchAsync (uri, content)
    }

let requestString host method = 
    task {
        use! res = request host method
        return! res.Content.ReadAsStringAsync()
    }
