module AspFeat.HttpContext

open System
open Microsoft.AspNetCore.Http

let readAsJson<'T> (ctx: HttpContext) = ctx.Request.ReadFromJsonAsync<'T>()

let write text (ctx: HttpContext) = ctx.Response.WriteAsync text
let writeTo ctx text = write text ctx

let writeAsJson<'T> (value: 'T) (ctx: HttpContext) = ctx.Response.WriteAsJsonAsync<'T> value
let writeAsJsonTo<'T> ctx (value: 'T) = writeAsJson<'T> value ctx

let setStatusCode (ctx: HttpContext) statusCode =
    ctx.Response.StatusCode <- statusCode

let setLocation (ctx: HttpContext) uri =
    ctx.Response.GetTypedHeaders().Location <- Uri (uri, UriKind.Relative)

let empty (ctx: HttpContext) = ctx.Response.CompleteAsync ()
let emptyWith ctx statusCode =
    setStatusCode ctx statusCode
    empty ctx

let noContent ctx = emptyWith ctx StatusCodes.Status204NoContent
let notFound  ctx = emptyWith ctx StatusCodes.Status404NotFound
let accepted  ctx = emptyWith ctx StatusCodes.Status202Accepted
let conflict  ctx = emptyWith ctx StatusCodes.Status409Conflict

let created ctx uri =
    setLocation ctx uri
    emptyWith ctx StatusCodes.Status201Created
let createdWith<'T> ctx uri value =
    setLocation ctx uri
    setStatusCode ctx StatusCodes.Status201Created
    writeAsJsonTo ctx value

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
