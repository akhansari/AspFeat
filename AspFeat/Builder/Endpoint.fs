[<RequireQualifiedAccess>]
module AspFeat.Builder.Endpoint

open Microsoft.AspNetCore.Routing

let feat (configureEndpoints: IEndpointRouteBuilder -> unit) : Feat =
    Feat.ignore, configureEndpoints
