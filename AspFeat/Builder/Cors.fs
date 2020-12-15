[<RequireQualifiedAccess>]
module AspFeat.Builder.Cors

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.Extensions.DependencyInjection

let private addServices (services: IServiceCollection) =
    services.AddCors ()

let private useMiddlewares configurePolicy (app: IApplicationBuilder) =
    app.UseCors(Action<CorsPolicyBuilder> configurePolicy)

let allowAny (builder: CorsPolicyBuilder) =
    builder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin()
    |> ignore

let feat configurePolicy =
    (addServices, useMiddlewares configurePolicy)
