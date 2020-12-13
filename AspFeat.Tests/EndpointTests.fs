module Endpoint

open System.Threading.Tasks
open FSharp.Control.Tasks
open Swensen.Unquote
open Xunit
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext
open System.Net.Http.Json
open System.Net.Http
open System.Net

[<Literal>]
let echo = "echo"
[<Literal>]
let icho = 123

type Echo = { Name: string }
let techo = { Name = echo }

[<Fact>]
let ``should write text`` () =
    unitTask {
        let configure bld = http bld Get "/" (write echo)

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get "/")
        content =! echo
    }

[<Fact>]
let ``should write json`` () =
    unitTask {
        let configure bld = http bld Get "/" (writeAsJson { Name = echo })

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get "/")

        content =! sprintf """{"name":"%s"}""" echo
    }

[<Fact>]
let ``should get single route value`` () =
    unitTask {
        let configure bld = httpf bld Get "/{p1}" write

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get $"/{echo}")

        content =! echo
    }

[<Fact>]
let ``should get tuple route values`` () =
    unitTask {
        let handler (str, id) = write $"{id}: {str}"
        let configure bld = httpf bld Get "/{p1}/{p2:int}" handler

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get $"/{echo}/{icho}")

        content =! $"{icho}: {echo}"
    }

[<Fact>]
let ``should get json content`` () =
    unitTask {
        let handler (content: Echo) ctx =
            content =! techo
            accepted ctx
        let configure bld = httpj bld Post "/" handler

        use! host = run [ Endpoint.feat configure ]
        use httpContent = JsonContent.Create techo :> HttpContent
        let! res = request host (RequestMethod.Post ("/", httpContent))

        res.StatusCode =! HttpStatusCode.Accepted
    }

[<Fact>]
let ``should send bad request if bad content type`` () =
    unitTask {
        let handler _ ctx = accepted ctx
        let configure bld = httpj bld Post "/" handler

        use! host = run [ Endpoint.feat configure ]
        use httpContent = new StringContent(echo) :> HttpContent
        let! res = request host (RequestMethod.Post ("/", httpContent))

        res.StatusCode =! HttpStatusCode.UnsupportedMediaType
    }

[<Fact>]
let ``should send bad request if bad json`` () =
    unitTask {
        let handler (_: Echo) ctx = accepted ctx
        let configure bld = httpj bld Post "/" handler

        use! host = run [ Endpoint.feat configure ]
        use httpContent = JsonContent.Create 500 :> HttpContent
        let! res = request host (RequestMethod.Post ("/", httpContent))

        res.StatusCode =! HttpStatusCode.BadRequest
    }

[<Fact>]
let ``should get route values and json content`` () =
    unitTask {
        let handler id (content: Echo) ctx =
            id =! icho
            content =! techo
            accepted ctx
        let configure bld = httpfj bld Post "/{p1}" handler

        use! host = run [ Endpoint.feat configure ]
        use httpContent = JsonContent.Create techo :> HttpContent
        let! res = request host (RequestMethod.Post ($"/{icho}", httpContent))

        res.StatusCode =! HttpStatusCode.Accepted
    }
