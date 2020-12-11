module AspFeat.Endpoint

open System
open System.Threading.Tasks
open Microsoft.FSharp.Reflection
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Routing.Patterns
open Microsoft.AspNetCore.Http

type MapHttpMethod =
    | Get
    | Post
    | Put
    | Delete

type HttpHandler = HttpContext -> Task

let mapHttp (bld: IEndpointRouteBuilder) (method: MapHttpMethod) pattern handler =
    bld.MapMethods(pattern, [ string method ], RequestDelegate handler)

let http bld method pattern handler =
    mapHttp bld method pattern handler |> ignore

let parserOf = function
    | t when t = typeof<int32>          -> int32                >> box
    | t when t = typeof<int64>          -> int64                >> box
    | t when t = typeof<decimal>        -> decimal              >> box
    | t when t = typeof<float>          -> float                >> box
    | t when t = typeof<single>         -> single               >> box
    | t when t = typeof<bool>           -> bool.Parse           >> box
    | t when t = typeof<DateTime>       -> DateTime.Parse       >> box
    | t when t = typeof<DateTimeOffset> -> DateTimeOffset.Parse >> box
    | t when t = typeof<Guid>           -> Guid.Parse           >> box
    | _                                 -> string               >> box

let httpf<'T> bld method pattern (handler: 'T -> HttpHandler) =
    let injectType = handler.GetType () |> FSharpType.GetFunctionElements |> fst
    let injectIsTuple = FSharpType.IsTuple injectType
    let parsers =
        if injectIsTuple
        then injectType.GenericTypeArguments |> Array.map parserOf
        else [| parserOf injectType |]
    let routeParams =
        RoutePatternFactory.Parse(pattern).Parameters
        |> Seq.mapi (fun i p -> (p.Name, parsers.[i])) |> Seq.toArray
    let wrapper ctx =
        let values = 
            routeParams |> Array.map (fun (name, parser) ->
                HttpContext.RouteValue.find ctx name |> parser)
        let inject =
            if injectIsTuple
            then FSharpValue.MakeTuple (values, injectType)
            else values.[0]
            :?> 'T
        handler inject ctx
    http bld method pattern wrapper
