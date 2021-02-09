[<RequireQualifiedAccess>]
module AspFeat.ProblemDetails

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc

let create status title detail =
    ProblemDetails
        (Status = Nullable status,
            Title = title,
            Detail = detail)

let statusOrDefaultOf (prob: ProblemDetails) =
    Option.ofNullable prob.Status
    |> Option.defaultValue StatusCodes.Status500InternalServerError

type Errors = Map<string, string list>

let validation errors =
    errors
    |> Map.map (fun _ v -> Array.ofList v)
    |> Map.toSeq
    |> dict
    |> ValidationProblemDetails

let appendErrors errors1 errors2 =
    Map.fold (fun acc key value1 ->
        match Map.tryFind key acc with
        | Some value2 -> Map.add key (value1 @ value2) acc
        | None        -> Map.add key value1            acc)
        errors1 errors2
