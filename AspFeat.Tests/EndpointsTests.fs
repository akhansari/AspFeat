module Tests

open Swensen.Unquote
open Xunit
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.Endpoint.Builder
open AspFeat.Endpoint.Response

[<Literal>]
let echo = "echo"

[<Fact>]
let ``should write text`` () = async {
    let configureEndpoints bld = get bld "/" (write echo)
    use! host = run [ Endpoint.featWith configureEndpoints ]
    let! content = requestString host (Get "/")
    content =! echo
}

[<Fact>]
let ``should write json`` () = async {
    let configureEndpoints bld = get bld "/" (writeAsJson {| Name = echo |})
    use! host = run [ Endpoint.featWith configureEndpoints ]
    let! content = requestString host (Get "/")
    content =! """{"name":"echo"}"""
}

[<Fact>]
let ``should get route value`` () = async {
    let handler ctx =
        RouteValue.find ctx "id"
        |> writeTo ctx
    let configureEndpoints bld = get bld "/{id}" handler
    use! host = run [ Endpoint.featWith configureEndpoints ]
    let! content = requestString host (Get $"/{echo}")
    content =! echo
}

[<Fact>]
let ``should get query strings`` () = async {
    let handler ctx =
        QueryString.toList ctx
        =! Map.ofList [ (echo, ["1"; "2"]) ]
        empty ctx
    let configureEndpoints bld = get bld "/" handler
    use! host = run [ Endpoint.featWith configureEndpoints ]
    do! requestString host (Get $"/?{echo}=1&{echo}=2") |> Async.Ignore
}

[<Fact>]
let ``should get only one query string value`` () = async {
    let handler ctx =
        QueryString.tryFindOne ctx echo =! Some "1"
        empty ctx
    let configureEndpoints bld = get bld "/" handler
    use! host = run [ Endpoint.featWith configureEndpoints ]
    do! requestString host (Get $"/?{echo}=1&{echo}=2") |> Async.Ignore
}