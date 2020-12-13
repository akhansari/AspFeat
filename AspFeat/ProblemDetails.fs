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
