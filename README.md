# AspFeat [![NuGet Status](http://img.shields.io/nuget/v/AspFeat.svg)](https://www.nuget.org/packages/AspFeat)

A modular and low ceremony toolkit for ASP.Net Core and F#.

- Modular injection of services and middlewares.
- Set of low ceremony ready-to-use setups.
- Functional helpers over ASP.Net Core and nothing else.
- Focused on Web APIs.

## Startup

In order to setup a feature properly, it's necessary to first add the services to `IServiceCollection` and then use the middlewares with `IApplicationBuilder`. The downside is that they are mixed with other features and moreover the order of calls are important, which makes everything complicated.

To keep the startup clean, the idea is to package features into modules and then expose the service collection and the application builder as a tuple.

The end result is that ASP.Net core startup has never been so easy:

```fsharp
[<EntryPoint>]
let main _ =
    let configure bld = http bld Get "/" (write "hello world")
    WebHost.run [ Endpoint.feat configure ]
```

Bts Implementation:

```fsharp
module Endpoint

let private addServices (services: IServiceCollection) =
    services.AddRouting()

let private useMiddlewares configureEndpoints (app: IApplicationBuilder) =
    app.UseRouting()
       .UseEndpoints(Action<IEndpointRouteBuilder> configureEndpoints)

let feat configureEndpoints =
    (addServices, useMiddlewares configureEndpoints)
```

## Endpoint Routing

AspFeat comes with several helpers that ease the use of the functional programming paradigm.\
We do not intend to completely change your way of using ASP.NET but rather to offer a more nice and more F#-idiomatic way of using ASP.NET.\
So you are not limited to AspFeat and you can still use Vanilla ASP.NET, if you need to.

### Example

**Without** AspFeat toolkit:
```fsharp
let configureEndpoints (bld: IEndpointRouteBuilder) =
    bld.MapGet("/", RequestDelegate getHandler) |> ignore
```

**With** AspFeat toolkit:
```fsharp
let configureEndpoints bld =
    http bld Get "/" getHandler
```

### Route values and JSON content injection

Instead of manually fetching data through `HttpContext`, it is possible to inject them into the handler.

- `httpf` injects route parameter values.
  - A single value is injected as is.
  - Multiple values are injected _in order_ as a tuple.
- `httpj` injects the deserialized JSON content.
- `httpfj` combines both.

```fsharp
let hello firstname = write $"Hello {firstname}"
let createGift gift = write $"Create a {gift}"
let goodbye (firstname, lastname) gift =
    write $"Goodbye {firstname} {lastname} and here is your {gift}"

let configureEndpoints bld =
    httpf  bld Get  "/hello/{firstname}" hello
    httpj  bld Post "/gift" createGift
    httpfj bld Put  "/goodbye/{firstname}/{lastname}" goodbye
```

You can find more examples in the samples folder.

### Handler Composition

It is possible to combine http handlers.

Normal with `=>`
```fsharp
let enrich (ctx: HttpContext) =
    ctx.Response.GetTypedHeaders().Set("X-Powered-By", "AspFeat")
    Task.CompletedTask

let configureEndpoints bld =
    http bld Get "/" (enrich => write "hello world")
```

Those with input injection are railwayed with `Result`.\
Then the `Error` type is `Map<string, string list>` and the response is a json of problem details with the status code 422 Unprocessable Entity.

Single value injection with `=|`
- `Ok` type could be:
  - A route parameter
  - A tuple of route parameters
  - A deserialized JSON model

```fsharp
let validateGetEcho id ctx =
    if id > 0
    then Ok id
    else Map [ ("Id", [ "Is negative or zero" ]) ] |> Error
    |> Task.FromResult

let getEcho id = write $"Echo {id}"

let configureEndpoints bld =
    httpf  bld Get  "/{id:int}" (validateGetEcho =|  getEcho)
```

Multiple values injection with `=||`
- `Ok` type is a tuple of two values:
  1. A route parameter or a tuple of route parameters
  1. Deserialized JSON model

```fsharp
let validateCreateEcho id name ctx =
    if not (String.IsNullOrWhiteSpace name)
    then Ok (id, {| Id = id; Name = name |})
    else Map [ ("Name", [ "Is empty" ]) ] |> Error
    |> Task.FromResult

let createEcho id model = createdWith $"/{id}" model

let configureEndpoints bld =
    httpfj bld Post "/{id:int}" (validateCreateEcho =|| createEcho)
```