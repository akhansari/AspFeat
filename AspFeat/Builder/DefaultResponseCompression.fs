[<RequireQualifiedAccess>]
module AspFeat.Builder.DefaultResponseCompression

open Microsoft.AspNetCore.Builder

let feat () : Feat =
    fun builder ->
        builder.Services
            .AddResponseCompression()
        |> ignore
    ,
    fun app ->
        app
            .UseResponseCompression()
        |> ignore
