module AspFeat.Endpoint

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Http

let get' (bld: IEndpointRouteBuilder) pattern handler =
    bld.MapGet(pattern, RequestDelegate handler)

let get bld pattern handler =
    get' bld pattern handler |> ignore
