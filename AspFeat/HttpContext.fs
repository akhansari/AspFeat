module AspFeat.HttpContext

open System
open System.Text.Json
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc

let setStatusCode (ctx: HttpContext) statusCode =
    ctx.Response.StatusCode <- statusCode

let setLocation (ctx: HttpContext) uri =
    ctx.Response.GetTypedHeaders().Location <- Uri (uri, UriKind.Relative)

let readAsJson<'T> (ctx: HttpContext) =
    task {
        try
            let! content = ctx.Request.ReadFromJsonAsync<'T>()
            return Ok content
        with
        | :? InvalidOperationException as e ->
            return
                ProblemDetails.create StatusCodes.Status415UnsupportedMediaType "Unsupported media type" e.Message
                |> Error
        | :? JsonException as e ->
            return
                ProblemDetails.create StatusCodes.Status400BadRequest "JSON parse error" e.Message
                |> Error
    }

let write text (ctx: HttpContext) = ctx.Response.WriteAsync text
let writeTo ctx text = write text ctx

let writeAsJson<'T> (value: 'T) (ctx: HttpContext) = ctx.Response.WriteAsJsonAsync<'T> value
let writeAsJsonTo<'T> ctx (value: 'T) = writeAsJson<'T> value ctx

let empty (ctx: HttpContext) = ctx.Response.StartAsync ()
let emptyWith statusCode ctx =
    setStatusCode ctx statusCode
    empty ctx

let noContent    ctx = emptyWith StatusCodes.Status204NoContent    ctx
let notFound     ctx = emptyWith StatusCodes.Status404NotFound     ctx
let accepted     ctx = emptyWith StatusCodes.Status202Accepted     ctx
let conflict     ctx = emptyWith StatusCodes.Status409Conflict     ctx
let forbidden    ctx = emptyWith StatusCodes.Status403Forbidden    ctx
let unauthorized ctx = emptyWith StatusCodes.Status401Unauthorized ctx

let created uri ctx =
    setLocation ctx uri
    emptyWith StatusCodes.Status201Created ctx
let createdWith<'T> uri value ctx =
    setLocation ctx uri
    setStatusCode ctx StatusCodes.Status201Created
    writeAsJsonTo ctx value

let writeProblemDetails prob ctx =
    setStatusCode ctx (ProblemDetails.statusOrDefaultOf prob)
    writeAsJsonTo ctx prob
let writeValidationError (prob: ValidationProblemDetails) ctx =
    setStatusCode ctx StatusCodes.Status422UnprocessableEntity
    writeAsJsonTo ctx prob

[<RequireQualifiedAccess>]
module RouteValue =

    let tryFind (ctx: HttpContext) key =
        match ctx.Request.RouteValues.TryGetValue key with
        | true, value -> string value |> Some
        | _ -> None
    
    let find ctx key =
        match tryFind ctx key with
        | Some value -> value
        | None -> failwithf "Route key missing: %s" key

[<RequireQualifiedAccess>]
module QueryString =

    let tryFindOne (ctx: HttpContext) key =
        match ctx.Request.Query.TryGetValue key with
        | true, values -> Seq.tryFind (String.IsNullOrEmpty >> not) values
        | _ -> None

    let toList (ctx: HttpContext) =
        ctx.Request.Query
        |> Seq.map (fun kv -> (kv.Key, Seq.toList kv.Value))
        |> Map.ofSeq
