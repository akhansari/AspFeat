[<RequireQualifiedAccess>]
module AspFeat.Builder.Endpoint

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing

let feat (configureEndpoints: IEndpointRouteBuilder -> unit) : Feat =
    fun builder ->
        builder.Services
            .AddResponseCompression()
        |> ignore
    ,
    fun app ->
        configureEndpoints app
        app
            .UseResponseCompression()
        |> ignore
