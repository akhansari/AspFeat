module AspFeat.Endpoint

open System
open System.Threading.Tasks
open System.Reflection
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Routing.Patterns
open AspFeat
open AspFeat.Reflect

/// HTTP request method
type HttpMethod =
    | Get
    | Post
    | Put
    | Delete

type HttpHandler = HttpContext -> Task
type ResultHttpHandler<'T,'TError> = HttpContext -> Task<Result<'T,'TError>>

let private paramNamesOf pattern =
    RoutePatternFactory.Parse(pattern).Parameters
    |> Seq.map (fun p -> p.Name) |> Seq.toArray

/// Adds an endpoint that matches HTTP requests for the specified HTTP method and pattern.
/// The handler is executed when the endpoint is matched.
let http (endpoints: IEndpointRouteBuilder) (method: HttpMethod) pattern handler =
    endpoints.MapMethods(pattern, [| string method |], RequestDelegate handler)

/// Adds an endpoint that matches HTTP requests for the specified HTTP method and pattern.
/// The handler is executed when the endpoint is matched.
/// The route values are injected as single value or tuple into the handler.
let httpf bld method pattern (handler: 'RouteValues -> HttpHandler) =
    let makeRouteValues = createTupleMaker<'RouteValues> (paramNamesOf pattern)
    let wrapper ctx =
        let routeValues = makeRouteValues (HttpContext.RouteValue.find ctx)
        handler routeValues ctx
    http bld method pattern wrapper

/// Adds an endpoint that matches HTTP requests for the specified HTTP method and pattern.
/// The handler is executed when the endpoint is matched.
/// The deserialized JSON content is injected into the handler.
let httpj bld method pattern (handler: 'JsonContent -> HttpHandler) =
    let wrapper ctx =
        task {
            match! HttpContext.readAsJson<'JsonContent> ctx with
            | Ok content -> do! handler content ctx
            | Error prob -> do! HttpContext.writeProblemDetails prob ctx
        }
        :> Task
    http bld method pattern wrapper

/// Adds an endpoint that matches HTTP requests for the specified HTTP method and pattern.
/// The handler is executed when the endpoint is matched.
/// The route values and deserialized JSON content are injected into the handler.
let httpfj bld method pattern (handler: 'RouteValues -> 'JsonContent -> HttpHandler) =
    let makeRouteValues = createTupleMaker<'RouteValues> (paramNamesOf pattern)
    let wrapper ctx =
        task {
            match! HttpContext.readAsJson<'JsonContent> ctx with
            | Ok content ->
                let routeValues = makeRouteValues (HttpContext.RouteValue.find ctx)
                do! handler routeValues content ctx
            | Error prob ->
                do! HttpContext.writeProblemDetails prob ctx
        }
        :> Task
    http bld method pattern wrapper

/// Like http function but the return value is ignored.
let uhttp   b m p h = http   b m p h |> ignore
/// Like httpf function but the return value is ignored.
let uhttpf  b m p h = httpf  b m p h |> ignore
/// Like httpj function but the return value is ignored.
let uhttpj  b m p h = httpj  b m p h |> ignore
/// Like httpfj function but the return value is ignored.
let uhttpfj b m p h = httpfj b m p h |> ignore

let private writeError errors =
    HttpContext.writeValidationError (ProblemDetails.validation errors)

/// (=>) Combine two HTTP handlers.
let combineHandler (handler1: HttpHandler) (handler2: HttpHandler) ctx =
    task {
        do! handler1 ctx
        if not ctx.Response.HasStarted then
            do! handler2 ctx
    }
    :> Task

/// (=|) Combine a single value HTTP handler with a normal one.
let combineHandler1 (lifter: 'T -> ResultHttpHandler<_,_>) (handler: 'U -> HttpHandler) model ctx =
    task {
        let! res = lifter model ctx
        match (ctx.Response.HasStarted, res) with
        | true , _            -> ()
        | false, Ok    mapped -> do! handler    mapped ctx
        | false, Error errors -> do! writeError errors ctx
    }
    :> Task

/// (=||) Combine a double value HTTP handler with a normal one.
let combineHandler2 (lifter: 'T1 -> 'T2 -> ResultHttpHandler<_,_>) (handler: 'U1 -> 'U2 -> HttpHandler) model1 model2 ctx =
    task {
        let! res = lifter model1 model2 ctx
        match (ctx.Response.HasStarted, res) with
        | true , _                     -> ()
        | false, Ok (mapped1, mapped2) -> do! handler mapped1 mapped2 ctx
        | false, Error errors          -> do! writeError errors ctx
    }
    :> Task

/// Combine two HTTP handlers.
let (=>)  = combineHandler
/// Combine a single value HTTP handler with a normal one.
let (=|)  = combineHandler1
/// Combine a double value HTTP handler with a normal one.
let (=||) = combineHandler2

/// Add metadata to assist with OpenAPI generation for the endpoint.
let addMetadataFromMethodInfo (conv: IEndpointConventionBuilder) (mi: MethodInfo) =
    conv.WithMetadata mi |> ignore
    for attr in mi.GetCustomAttributes() do
        conv.WithMetadata attr |> ignore
    let reqDelegateResult = RequestDelegateFactory.Create mi
    for metadata in reqDelegateResult.EndpointMetadata do
        conv.WithMetadata metadata |> ignore
    conv

/// Endpoints computation expression builder.
type EndpointsBuilder (builder: IEndpointRouteBuilder) =

    member _.Yield _ = ()

    [<CustomOperation "get">]
    member _.Get (_, pattern, handler) = uhttp  builder Get pattern handler
    [<CustomOperation "get">]
    member _.Get (_, pattern, handler) = uhttpf builder Get pattern handler

    [<CustomOperation "delete">]
    member _.Delete (_, pattern, handler) = uhttp  builder Delete pattern handler
    [<CustomOperation "delete">]
    member _.Delete (_, pattern, handler) = uhttpf builder Delete pattern handler

    [<CustomOperation "post">]
    member _.Post  (_, pattern, handler) = uhttp   builder Post pattern handler
    [<CustomOperation "postEmpty">]
    member _.Postf (_, pattern, handler) = uhttpf  builder Post pattern handler
    [<CustomOperation "post">]
    member _.Post  (_, pattern, handler) = uhttpj  builder Post pattern handler
    [<CustomOperation "post">]
    member _.Post  (_, pattern, handler) = uhttpfj builder Post pattern handler

    [<CustomOperation "put">]
    member _.Put (_, pattern, handler) = uhttp   builder Put pattern handler
    [<CustomOperation "put">]
    member _.Put (_, pattern, handler) = uhttpj  builder Put pattern handler
    [<CustomOperation "put">]
    member _.Put (_, pattern, handler) = uhttpfj builder Put pattern handler

/// Builds endpoints using computation expression syntax.
let endpoints b = EndpointsBuilder b

/// Endpoints with enriched metadata computation expression builder to assist with OpenAPI generation.
type EndpointsMetadataBuilder<'T> (builder: IEndpointRouteBuilder) =

    let group = typeof<'T>

    let spec methodName (conv: IEndpointConventionBuilder) =
        group.GetMethods()
        |> Array.tryFind (fun m -> String.Compare(m.Name, methodName, true) = 0)
        |> function
        | Some mi -> addMetadataFromMethodInfo conv mi
        | None -> failwith $"Method '{methodName}' not found in '{group.Name}' type."
        |> ignore

    member _.Yield _ = ()

    [<CustomOperation "get">]
    member _.Get (_, pattern, handler, methodName) = http  builder Get pattern handler |> spec methodName
    [<CustomOperation "get">]
    member _.Get (_, pattern, handler, methodName) = httpf builder Get pattern handler |> spec methodName

    [<CustomOperation "delete">]
    member _.Delete (_, pattern, handler, methodName) = http  builder Delete pattern handler |> spec methodName
    [<CustomOperation "delete">]
    member _.Delete (_, pattern, handler, methodName) = httpf builder Delete pattern handler |> spec methodName

    [<CustomOperation "post">]
    member _.Post  (_, pattern, handler, methodName) = http   builder Post pattern handler |> spec methodName
    [<CustomOperation "postEmpty">]
    member _.Postf (_, pattern, handler, methodName) = httpf  builder Post pattern handler |> spec methodName
    [<CustomOperation "post">]
    member _.Post  (_, pattern, handler, methodName) = httpj  builder Post pattern handler |> spec methodName
    [<CustomOperation "post">]
    member _.Post  (_, pattern, handler, methodName) = httpfj builder Post pattern handler |> spec methodName

    [<CustomOperation "put">]
    member _.Put (_, pattern, handler, methodName) = http   builder Put pattern handler |> spec methodName
    [<CustomOperation "put">]
    member _.Put (_, pattern, handler, methodName) = httpj  builder Put pattern handler |> spec methodName
    [<CustomOperation "put">]
    member _.Put (_, pattern, handler, methodName) = httpfj builder Put pattern handler |> spec methodName

/// Builds endpoints with enriched metadata to assist with OpenAPI generation using computation expression syntax.
/// 'T should be an interface with methods having the same signature as handlers.
/// Methods and types can be enriched with attributes.
let endpointsMetadata<'T> b = EndpointsMetadataBuilder<'T> b
