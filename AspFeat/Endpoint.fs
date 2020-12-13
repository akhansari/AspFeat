module AspFeat.Endpoint

open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Routing.Patterns
open AspFeat
open AspFeat.Reflect

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

let private paramNamesOf pattern =
    RoutePatternFactory.Parse(pattern).Parameters
    |> Seq.map (fun p -> p.Name) |> Seq.toArray

let httpf bld method pattern (handler: 'RouteValues -> HttpHandler) =
    let makeRouteValues = createTupleMaker<'RouteValues> (paramNamesOf pattern)
    let wrapper ctx =
        let routeValues = makeRouteValues (HttpContext.RouteValue.find ctx)
        handler routeValues ctx
    http bld method pattern wrapper

let httpj bld method pattern (handler: 'JsonContent -> HttpHandler) =
    let wrapper ctx =
        unitTask {
            match! HttpContext.readAsJson<'JsonContent> ctx with
            | Ok content -> do! handler content ctx
            | Error prob -> do! HttpContext.writeProblemDetails prob ctx
        }
    http bld method pattern wrapper

let httpfj bld method pattern (handler: 'RouteValues -> 'JsonContent -> HttpHandler) =
    let makeRouteValues = createTupleMaker<'RouteValues> (paramNamesOf pattern)
    let wrapper ctx =
        unitTask {
            match! HttpContext.readAsJson<'JsonContent> ctx with
            | Ok content ->
                let routeValues = makeRouteValues (HttpContext.RouteValue.find ctx)
                do! handler routeValues content ctx
            | Error prob ->
                do! HttpContext.writeProblemDetails prob ctx
        }
    http bld method pattern wrapper
