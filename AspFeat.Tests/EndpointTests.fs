module Endpoint

open FSharp.Control.Tasks
open Swensen.Unquote
open Xunit
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext

[<Literal>]
let echo = "echo"

[<Fact>]
let ``should write text`` () =
    task {
        let configureEndpoints bld = http bld Get "/" (write echo)
        use! host = run [ Endpoint.feat configureEndpoints ]
        let! content = requestString host (RequestMethod.Get "/")
        content =! echo
    }

[<Fact>]
let ``should write json`` () =
    task {
        let configureEndpoints bld = http bld Get "/" (writeAsJson {| Name = echo |})
        use! host = run [ Endpoint.feat configureEndpoints ]
        let! content = requestString host (RequestMethod.Get "/")
        content =! """{"name":"echo"}"""
    }
