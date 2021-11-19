namespace AspFeat.Builder

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

type Feat =
    ( (WebApplicationBuilder -> unit) *
      (WebApplication -> unit) )
module Feat =
    let ignore _ = ()

[<RequireQualifiedAccess>]
module WebApp =

    let createWithArgs (args: string array) (feats: Feat seq) =
        let builder = WebApplication.CreateBuilder args

        for (setup, _) in feats do
            setup builder

        let app = builder.Build()

        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        for (_, setup) in feats do
            setup app

        app

    let create feats =
        createWithArgs Array.empty feats

    let run args feats =
        let app = createWithArgs args feats
        app.Run()
        0
