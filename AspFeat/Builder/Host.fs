namespace AspFeat.Builder

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module Host =

    let addConsole (host: IHostBuilder) =
        host.ConfigureLogging(fun b -> b.AddConsole() |> ignore)

    let run (host: IHostBuilder) =
        host.Build().Run()
        0

    let start (host: IHostBuilder) =
        host.Build().StartAsync()
