/// Configuration helpers
[<AutoOpen>]
module AspFeat.Extensions

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

type HttpContext with

    // Environment

    member this.GetEnvironment () =
        this.RequestServices.GetRequiredService<IWebHostEnvironment>()

    // Configuration

    member this.GetConf () =
        this.RequestServices.GetRequiredService<IConfiguration>()

    member this.GetConfValue<'T> key =
        this.GetConf().GetValue<'T> key

    member this.GetConfValueOrDefault<'T> key defaultValue =
        this.GetConf().GetValue<'T>(key, defaultValue)

    member this.GetConfString key =
        match this.GetConf().GetValue<string> key with
        | null -> failwith $"The key '{key}' not found in the settings."
        | value -> value

    member this.TryGetConfString key =
        this.GetConf().GetValue<string> key |> Option.ofObj

    // Logger

    member this.CreateLogger (categoryName: string) =
        this.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger categoryName

    member this.GetLogger<'T> () =
        this.RequestServices.GetRequiredService<ILogger<'T>>()

    member this.LogInfo<'T> message =
        this.GetLogger<'T>().LogInformation message
