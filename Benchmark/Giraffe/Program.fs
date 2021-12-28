open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.EndpointRouting

type Hello = { Hello: string }

let handler : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.WriteJsonAsync { Hello = "Giraffe" }

[<EntryPoint>]
let main args =
    let endpoints = [ GET [ route "/hello" handler ] ]
    let builder = WebApplication.CreateBuilder args
    builder.Logging.ClearProviders() |> ignore
    builder.Services.AddResponseCompression().AddRouting().AddGiraffe() |> ignore
    let app = builder.Build()
    app.UseResponseCompression().UseRouting().UseGiraffe(endpoints) |> ignore
    app.Run()
    0
