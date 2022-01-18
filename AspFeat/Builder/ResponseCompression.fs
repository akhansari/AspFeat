[<RequireQualifiedAccess>]
module AspFeat.Builder.ResponseCompression

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.ResponseCompression

let feat configureOptions : Feat =
    fun builder ->
        builder.Services
            .AddResponseCompression(Action<ResponseCompressionOptions> configureOptions)
        |> ignore
    ,
    fun app ->
        app
            .UseResponseCompression()
        |> ignore
