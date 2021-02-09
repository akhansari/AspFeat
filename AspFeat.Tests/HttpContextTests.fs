module HttpContext

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open Swensen.Unquote
open Xunit
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext

let echo = "echo"

[<Fact>]
let ``should get route value`` () =
    unitTask {
        let handler ctx =
            RouteValue.find ctx "id" =! echo
            noContent ctx
        let configureEndpoints bld = http bld Get "/{id}" handler
        use! host = run [ Endpoint.feat configureEndpoints ]
        do! request host (RequestMethod.Get $"/{echo}") :> Task
    }

[<Fact>]
let ``should get query strings`` () = 
    unitTask {
        let handler ctx =
            QueryString.toList ctx
            =! Map.ofList [ (echo, ["1"; "2"]) ]
            noContent ctx
        let configureEndpoints bld = http bld Get "/" handler
        use! host = run [ Endpoint.feat configureEndpoints ]
        do! request host (RequestMethod.Get $"/?{echo}=1&{echo}=2") :> Task
    }

[<Fact>]
let ``should get only one query string value`` () =
    unitTask {
        let handler ctx =
            QueryString.tryFindOne ctx echo =! Some "1"
            noContent ctx
        let configureEndpoints bld = http bld Get "/" handler
        use! host = run [ Endpoint.feat configureEndpoints ]
        do! request host (RequestMethod.Get $"/?{echo}=1&{echo}=2") :> Task
    }

[<Fact>]
let ``should write location`` () =
    unitTask {
        let handler (ctx: HttpContext) =
            setLocation ctx echo
            noContent ctx
        let configureEndpoints bld = http bld Get "/" handler
        use! host = run [ Endpoint.feat configureEndpoints ]
        let! res = request host (RequestMethod.Get "/")
        string res.Headers.Location =! echo
    }