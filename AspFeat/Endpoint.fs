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

let httpf<'T> bld method pattern (handler: 'T -> HttpHandler) =
    let getRouteInjection =
        RoutePatternFactory.Parse(pattern).Parameters
        |> Seq.map (fun p -> p.Name) |> Seq.toArray
        |> makeTupleInjection<'T>
    let wrapper ctx =
        let inject = getRouteInjection (HttpContext.RouteValue.find ctx)
        handler inject ctx
    http bld method pattern wrapper

let private readJson<'T> ctx =
    task {
        try
            let! model = HttpContext.readAsJson<'T> ctx
            return Ok model
        with e ->
            return
                ProblemDetails.create StatusCodes.Status400BadRequest "JSON parse error" e.Message
                |> Error
    }

let httpj<'T> bld method pattern (handler: 'T -> HttpHandler) =
    let wrapper (ctx: HttpContext) =
        unitTask {
            if ctx.Request.HasJsonContentType () then
                match! readJson<'T> ctx with
                | Ok model -> do! handler model ctx
                | Error prob -> do! HttpContext.writeProblemDetails ctx prob
            else
                do! HttpContext.emptyWith ctx StatusCodes.Status415UnsupportedMediaType
        }
    http bld method pattern wrapper
