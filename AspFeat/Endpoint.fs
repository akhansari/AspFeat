module AspFeat.Endpoint

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Http

type MapHttpMethod =
    | Get
    | Post
    | Put
    | Delete

let mapHttp (bld: IEndpointRouteBuilder) method pattern handler =
    bld.MapMethods(pattern, [ string method ], RequestDelegate handler)

let http bld pattern method handler =
    mapHttp bld pattern method handler |> ignore
