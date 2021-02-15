module AspFeat.Reflect

open System
open Microsoft.FSharp.Reflection

let private parsers =
    [ (typeof<int32>         , int32                >> box)
      (typeof<int64>         , int64                >> box)
      (typeof<decimal>       , decimal              >> box)
      (typeof<float>         , float                >> box)
      (typeof<float32>       , float32              >> box)
      (typeof<bool>          , bool.Parse           >> box)
      (typeof<DateTime>      , DateTime.Parse       >> box)
      (typeof<DateTimeOffset>, DateTimeOffset.Parse >> box)
      (typeof<Guid>          , Guid.Parse           >> box) ]
    |> readOnlyDict

let private parserOf t =
    match parsers.TryGetValue t with
    | true, parser -> parser
    | _            -> string >> box

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


let private defaultValues =
    [ (typeof<unit>          , box ()      )
      (typeof<string>        , box "string")
      (typeof<char>          , box 'c'     )
      (typeof<bool>          , box false   )
      (typeof<byte>          , box 0uy     )
      (typeof<sbyte>         , box 0y      )
      (typeof<int16>         , box 0s      )
      (typeof<uint16>        , box 0us     )
      (typeof<int32>         , box 0       )
      (typeof<uint32>        , box 0u      )
      (typeof<int64>         , box 0L      )
      (typeof<uint64>        , box 0UL     )
      (typeof<decimal>       , box 0m      )
      (typeof<float>         , box 0.0     )
      (typeof<float32>       , box 0f      )
      (typeof<Guid>          , box Guid.Empty             )
      (typeof<DateTime>      , box DateTime.MinValue      )
      (typeof<DateTimeOffset>, box DateTimeOffset.MinValue) ]
    |> readOnlyDict

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

    if  defaultValues.ContainsKey t then
        defaultValues.[t]

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
