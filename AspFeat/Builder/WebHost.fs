namespace AspFeat.Builder

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http.Json
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open AspFeat

type Feat =
    ( (IServiceCollection -> IServiceCollection) *
      (IApplicationBuilder -> IApplicationBuilder) )

[<RequireQualifiedAccess>]
module WebHost =

    let private configureServices feats
        (_: WebHostBuilderContext) (services: IServiceCollection) =

        services.Configure(fun (o: JsonOptions) ->
            JsonSerializer.setupDefaultOptions o.SerializerOptions |> ignore)
        |> ignore

        for (setup, _) in feats do
            setup services |> ignore

    let private configureApp feats
        (context: WebHostBuilderContext) (app: IApplicationBuilder) =

        if context.HostingEnvironment.IsDevelopment () then
            app.UseDeveloperExceptionPage () |> ignore

        for (_, setup) in feats do
            setup app |> ignore

    let configure (feats: Feat list) (builder: IWebHostBuilder) =
        builder
            .ConfigureServices(configureServices feats)
            .Configure(configureApp feats)

    let create (extend: IWebHostBuilder -> IWebHostBuilder) feats =
        HostBuilder()
            .ConfigureWebHost(fun builder ->
                builder
                |> extend
                |> configure feats
                |> ignore)

    let run feats =
        feats
        |> create (fun b -> b.UseKestrel())
        |> Host.addConsole
        |> Host.run
