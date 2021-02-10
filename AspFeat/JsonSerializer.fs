[<RequireQualifiedAccess>]
module AspFeat.JsonSerializer

open System.Text.Json
open System.Text.Json.Serialization

let setupDefaultOptions (opt: JsonSerializerOptions) =
    opt.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    opt.Converters.Add(JsonStringEnumConverter())
    opt.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.FSharpLuLike,
            unionTagCaseInsensitive = true,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase))
    opt

let createDefaultOptions () =
    JsonSerializerOptions ()
    |> setupDefaultOptions
