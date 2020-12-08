module HttpContext

open Microsoft.AspNetCore.Http
open Swensen.Unquote
open Xunit
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext
open AspFeat.HttpContext.Response
open System

[<Literal>]
let echo = "echo"

[<Fact>]
let ``should get route value`` () = async {
    let handler ctx =
        RouteValue.find ctx "id" =! echo
        empty ctx
    let configureEndpoints bld = get bld "/{id}" handler
    use! host = run [ Endpoint.feat configureEndpoints ]
    do! requestString host (Get $"/{echo}") |> Async.Ignore
}

[<Fact>]
let ``should get query strings`` () = async {
    let handler ctx =
        QueryString.toList ctx
        =! Map.ofList [ (echo, ["1"; "2"]) ]
        empty ctx
    let configureEndpoints bld = get bld "/" handler
    use! host = run [ Endpoint.feat configureEndpoints ]
    do! requestString host (Get $"/?{echo}=1&{echo}=2") |> Async.Ignore
}

[<Fact>]
let ``should get only one query string value`` () = async {
    let handler ctx =
        QueryString.tryFindOne ctx echo =! Some "1"
        empty ctx
    let configureEndpoints bld = get bld "/" handler
    use! host = run [ Endpoint.feat configureEndpoints ]
    do! requestString host (Get $"/?{echo}=1&{echo}=2") |> Async.Ignore
}

[<Fact>]
let ``should write location`` () = async {
    let handler (ctx: HttpContext) =
        Header.setLocation ctx echo
        empty ctx
    let configureEndpoints bld = get bld "/" handler
    use! host = run [ Endpoint.feat configureEndpoints ]
    let! res = request host (Get "/")
    string res.Headers.Location =! echo
}