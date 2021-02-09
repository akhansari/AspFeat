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
type ResultHttpHandler<'T,'TError> = HttpContext -> Task<Result<'T,'TError>>

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

let private writeError errors =
    HttpContext.writeValidationError (ProblemDetails.validation errors)

let combineHandler (handler1: HttpHandler) (handler2: HttpHandler) ctx =
    unitTask {
        do! handler1 ctx
        if not ctx.Response.HasStarted then
            do! handler2 ctx
    }

let combineHandler1 (lifter: 'T -> ResultHttpHandler<_,_>) (handler: 'U -> HttpHandler) model ctx =
    unitTask {
        let! res = lifter model ctx
        match (ctx.Response.HasStarted, res) with
        | true , _            -> ()
        | false, Ok    mapped -> do! handler    mapped ctx
        | false, Error errors -> do! writeError errors ctx
    }

let combineHandler2 (lifter: 'T1 -> 'T2 -> ResultHttpHandler<_,_>) (handler: 'U1 -> 'U2 -> HttpHandler) model1 model2 ctx =
    unitTask {
        let! res = lifter model1 model2 ctx
        match (ctx.Response.HasStarted, res) with
        | true , _                     -> ()
        | false, Ok (mapped1, mapped2) -> do! handler mapped1 mapped2 ctx
        | false, Error errors          -> do! writeError errors ctx
    }

let (=>)  = combineHandler
let (=|)  = combineHandler1
let (=||) = combineHandler2
