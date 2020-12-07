namespace AspFeat.Endpoint

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Http

module Builder =

    let get' (bld: IEndpointRouteBuilder) pattern handler =
        bld.MapGet(pattern, RequestDelegate handler)
    let get bld pattern handler =
        get' bld pattern handler |> ignore

module Response =

    let empty (ctx: HttpContext) =
        ctx.Response.CompleteAsync ()

    let emptyWith (ctx: HttpContext) statusCode =
        ctx.Response.StatusCode <- statusCode
        empty ctx

    let write text (ctx: HttpContext) =
        ctx.Response.WriteAsync text
    let writeTo ctx text =
        write text ctx

    let writeAsJson<'T> (value: 'T) (ctx: HttpContext) =
        ctx.Response.WriteAsJsonAsync<'T> value
    let writeAsJsonTo<'T> (ctx: HttpContext) (value: 'T) =
        writeAsJson value ctx

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
