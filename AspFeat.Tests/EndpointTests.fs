module Endpoint

open System.Net
open System.Net.Http.Json
open System.Net.Http
open System.Threading.Tasks

open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Swensen.Unquote
open Xunit

open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext

let echo = "echo"
let necho = 42
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
        let handler (p1, p2) = write $"{p2}: {p1}"
        let configure bld = httpf bld Get "/{p1}/{p2:int}" handler

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get $"/{echo}/{necho}")

        content =! $"{necho}: {echo}"
    }

[<Fact>]
let ``should get json content`` () =
    unitTask {
        let handler (model: Echo) ctx =
            model =! techo
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
        let handler p1 (model: Echo) ctx =
            p1 =! necho
            model =! techo
            accepted ctx
        let configure bld = httpfj bld Post "/{p1}" handler

        use! host = run [ Endpoint.feat configure ]
        use httpContent = JsonContent.Create techo :> HttpContent
        let! res = request host (RequestMethod.Post ($"/{necho}", httpContent))

        res.StatusCode =! HttpStatusCode.Accepted
    }

[<Fact>]
let ``should chain http handlers`` () =
    unitTask {
        let enrich (ctx: HttpContext) =
            ctx.Response.GetTypedHeaders().Set("foo", "bar")
            Task.CompletedTask
        let configure bld = http bld Get "/" (enrich => write echo)

        use! host = run [ Endpoint.feat configure ]
        let! res = request host (RequestMethod.Get $"/")

        res.Headers.GetValues "foo" |> List.ofSeq =! [ "bar" ]
        let! content = res.Content.ReadAsStringAsync ()
        content =! echo
    }

[<Fact>]
let ``should break http handlers chaining if response already started`` () =
    unitTask {
        let enrich (ctx: HttpContext) =
            ctx.Response.StatusCode <- 206
            Task.CompletedTask
        let configure bld = http bld Get "/" (enrich => write echo => write "foo")

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get $"/")

        content =! echo
    }

[<Fact>]
let ``should chain single value http handlers`` () =
    unitTask {
        let validate p1 _ = Ok p1 |> Task.FromResult
        let configure bld = httpf bld Get "/{p1}" (validate =| write)

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get $"/{echo}")

        content =! echo
    }

[<Fact>]
let ``should break single value http handlers chaining on error`` () =
    unitTask {
        let validate _ _ = Error Map.empty |> Task.FromResult
        let configure bld = httpf bld Get "/{p1}" (validate =| write)

        use! host = run [ Endpoint.feat configure ]
        let! res = request host (RequestMethod.Get $"/{necho}")

        res.StatusCode =! HttpStatusCode.UnprocessableEntity
    }

[<Fact>]
let ``should break single value http handlerse chaining if response already started`` () =
    unitTask {
        let authenticate p1 ctx =
            task {
                do! forbidden ctx
                return Ok p1
            }
        let configure bld = httpf bld Get "/{p1}" (authenticate =| write)

        use! host = run [ Endpoint.feat configure ]
        let! content = requestString host (RequestMethod.Get $"/{echo}")

        content <>! echo
    }

[<Fact>]
let ``should chain double value http handlers`` () =
    unitTask {
        let validate p1 model _ = Ok (p1, model) |> Task.FromResult
        let handler _ _ = accepted
        let configure bld = httpfj bld Post "/{p1}" (validate =|| handler)

        use! host = run [ Endpoint.feat configure ]
        use httpContent = JsonContent.Create techo :> HttpContent
        let! res = request host (RequestMethod.Post ($"/{necho}", httpContent))

        res.StatusCode =! HttpStatusCode.Accepted
    }

[<Fact>]
let ``should break double value http handlers chaining on error`` () =
    unitTask {
        let validate _ _ _ = Error Map.empty |> Task.FromResult
        let handler _ _ = accepted
        let configure bld = httpfj bld Post "/{p1}" (validate =|| handler)

        use! host = run [ Endpoint.feat configure ]
        use httpContent = JsonContent.Create techo :> HttpContent
        let! res = request host (RequestMethod.Post ($"/{necho}", httpContent))

        res.StatusCode =! HttpStatusCode.UnprocessableEntity
    }

[<Fact>]
let ``should break double value http handlers chaining if response already started`` () =
    unitTask {
        let authenticate p1 model ctx =
            task {
                do! forbidden ctx
                return Ok (p1, model)
            }
        let handler _ _ = accepted
        let configure bld = httpfj bld Post "/{p1}" (authenticate =|| handler)

        use! host = run [ Endpoint.feat configure ]
        use httpContent = JsonContent.Create techo :> HttpContent
        let! res = request host (RequestMethod.Post ($"/{necho}", httpContent))

        res.StatusCode =! HttpStatusCode.Forbidden
    }
