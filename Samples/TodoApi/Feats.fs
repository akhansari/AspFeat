namespace TodoApi

open AspFeat.Builder


[<RequireQualifiedAccess>]
module Swagger =
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection

    let feat () : Feat =
        fun builder ->
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen()
            |> ignore
        ,
        fun app ->
            app
                .UseSwagger()
                .UseSwaggerUI()
            |> ignore
