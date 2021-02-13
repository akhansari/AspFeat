module AspFeat.Reflect

open System
open Microsoft.FSharp.Reflection


let private parserOf = function
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

type ElemName  = string
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


let rec private defaultValueOf depth (t: Type) =

    let getDepth () = Map.tryFind t.GUID depth |> defaultArg <| 0
    let incrDepth () =
        match Map.tryFind t.GUID depth with
        | Some l -> Map.add t.GUID (l + 1) depth
        | None   -> Map.add t.GUID 1 depth

    let makeUnion (uc: UnionCaseInfo) =
        let d = incrDepth ()
        let values = uc.GetFields () |> Array.map (fun p -> defaultValueOf d p.PropertyType)
        FSharpValue.MakeUnion (uc, values)
    let findUnionCase name =
        FSharpType.GetUnionCases t |> Array.find (fun u -> u.Name = name)

    if   t = typeof<unit>    then box ()
    elif t = typeof<string>  then box "string"
    elif t = typeof<char>    then box 'c'
    elif t = typeof<bool>    then box false
    elif t = typeof<byte>    then box 0uy
    elif t = typeof<sbyte>   then box 0y
    elif t = typeof<int16>   then box 0s
    elif t = typeof<uint16>  then box 0us
    elif t = typeof<int32>   then box 0
    elif t = typeof<uint32>  then box 0u
    elif t = typeof<int64>   then box 0L
    elif t = typeof<uint64>  then box 0UL
    elif t = typeof<decimal> then box 0m
    elif t = typeof<float>   then box 0.0
    elif t = typeof<float32> then box 0f

    elif t = typeof<Guid>           then box Guid.Empty
    elif t = typeof<DateTime>       then box DateTime.MinValue
    elif t = typeof<DateTimeOffset> then box DateTimeOffset.MinValue

    elif FSharpType.IsUnion t then
        if t.IsGenericType && typedefof<Option<_>> = t.GetGenericTypeDefinition () then
            if getDepth () < 1
            then findUnionCase "Some" |> makeUnion
            else findUnionCase "None" |> makeUnion
        else
            let ucs = FSharpType.GetUnionCases t
            makeUnion ucs.[getDepth ()]

    elif FSharpType.IsTuple t then
        FSharpValue.MakeTuple (Array.map (defaultValueOf depth) t.GenericTypeArguments, t)

    elif FSharpType.IsRecord t then
        let values =
            FSharpType.GetRecordFields t
            |> Array.map (fun p -> defaultValueOf depth p.PropertyType)
        FSharpValue.MakeRecord (t, values)

    else
        NotImplementedException t.FullName |> raise

let makeSample<'T> () =
    defaultValueOf Map.empty typeof<'T> |> unbox<'T>
