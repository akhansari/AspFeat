[<RequireQualifiedAccess>]
module AspFeat.Builder.Cors

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.Extensions.DependencyInjection

let private addCors (services: IServiceCollection) =
    services.AddCors ()

let private useCors configurePolicy (app: IApplicationBuilder) =
    app.UseCors(Action<CorsPolicyBuilder> configurePolicy)

let allowAny (builder: CorsPolicyBuilder) =
    builder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin()
    |> ignore

let feat configurePolicy =
    (addCors, useCors configurePolicy)
