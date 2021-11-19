module internal AspFeat.Reflect
[<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AspFeat.Tests")>] do ()

open System
open Microsoft.FSharp.Reflection

let parserOf = function
    | t when t = typeof<int32>          -> int32                >> box
    | t when t = typeof<int64>          -> int64                >> box
    | t when t = typeof<decimal>        -> decimal              >> box
    | t when t = typeof<float>          -> float                >> box
    | t when t = typeof<float32>        -> float32              >> box
    | t when t = typeof<bool>           -> bool.Parse           >> box
    | t when t = typeof<DateTime>       -> DateTime.Parse       >> box
    | t when t = typeof<DateTimeOffset> -> DateTimeOffset.Parse >> box
    | t when t = typeof<Guid>           -> Guid.Parse           >> box
    | _                                 -> string               >> box

let private makeTuple tupleType values = FSharpValue.MakeTuple (values, tupleType)

type ElemName = string
type ElemValue = string

let createTupleMaker<'T> elemNames =
    let injectType = typeof<'T>
    let injectIsTuple = FSharpType.IsTuple injectType
    let parsers =
        if injectIsTuple
        then injectType.GenericTypeArguments |> Array.map parserOf
        else [| parserOf injectType |]
    let routeParams = Array.zip elemNames parsers
    if Array.isEmpty routeParams then failwith "No route parameters"
    let makeType =
        if injectIsTuple
        then makeTuple injectType
        else Array.head
    fun (valueOf: ElemName -> ElemValue) ->
        routeParams
        |> Array.map (fun (name, parser) -> valueOf name |> parser)
        |> makeType
        |> unbox<'T>

