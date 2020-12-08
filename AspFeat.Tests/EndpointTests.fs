module Endpoint

open Swensen.Unquote
open Xunit
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext.Response

[<Literal>]
let echo = "echo"

[<Fact>]
let ``should write text`` () = async {
    let configureEndpoints bld = get bld "/" (write echo)
    use! host = run [ Endpoint.feat configureEndpoints ]
    let! content = requestString host (Get "/")
    content =! echo
}

[<Fact>]
let ``should write json`` () = async {
    let configureEndpoints bld = get bld "/" (writeAsJson {| Name = echo |})
    use! host = run [ Endpoint.feat configureEndpoints ]
    let! content = requestString host (Get "/")
    content =! """{"name":"echo"}"""
}
