# AspFeat [![NuGet Status](http://img.shields.io/nuget/v/AspFeat.svg)](https://www.nuget.org/packages/AspFeat) ![ASP.NET Core 6.0](https://img.shields.io/badge/ASP.NET%20Core-6.0-blue)

A modular and low ceremony toolkit for ASP .Net and F#.

- Modular injection of services and middlewares.
- Set of low ceremony ready-to-use setups.
- Functional helpers over ASP .Net and nothing else.
- Focused on Web APIs.

You can find examples in the samples folder.

## Startup

In order to setup a feature properly, it's necessary to first add the [services](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection) to `IServiceCollection` and then use the [middlewares](https://docs.microsoft.com/aspnet/core/fundamentals/middleware/) with `IApplicationBuilder`. The downside is that they are mixed with other features and moreover the order of calls are important, which makes everything complicated.

To keep the startup clean, the idea is to package features into modules and then expose the setup of `WebApplicationBuilder` and `WebApplication` as a tuple.

The end result is that ASP .Net startup has never been so easy:

```fsharp
[<EntryPoint>]
let main args =
    let configure bld = uhttp bld Get "/" (write "hello world")
    WebApp.run args [ Endpoint.feat configure ]
```

Swagger sample:

```fsharp
module Swagger =
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection

    let feat () : Feat =
        fun builder ->
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen()
            |> ignore
        ,
        fun app ->
            app
                .UseSwagger()
                .UseSwaggerUI()
            |> ignore

[<EntryPoint>]
let main args =
    [ Endpoint.feat configureEndpoints
      Swagger.feat () ]
    |> WebApp.run args
```

## Endpoint Routing

AspFeat comes with several helpers that ease the use of the functional programming paradigm.\
We do not intend to completely change your way of using ASP .Net but rather to offer a more nice and more F#-idiomatic way of using ASP .Net.\
So you are not limited to AspFeat and you can still use Vanilla ASP .Net, if you need to.

For further information please refer to [Microsoft Docs](https://docs.microsoft.com/aspnet/core/fundamentals/routing)

### Example

**Without** AspFeat toolkit:
```fsharp
let configureEndpoints (bld: IEndpointRouteBuilder) =
    bld.MapGet("/", RequestDelegate getHandler) |> ignore
```

**With** AspFeat toolkit:
```fsharp
let configureEndpoints bld =
    uhttp bld Get "/" getHandler
```

With the DSL:
```fsharp
let configureEndpoints bld =
    endpoints bld {
        get "/" getHandler
    }
    |> ignore
```

With the OpenApi/Swagger DSL:
```fsharp
let getHandler =
    writeAsJson "hello world"

type World =
    [<ProducesResponseType(StatusCodes.Status200OK)>]
    abstract member GetHandler: unit -> string

let configureEndpoints bld =
    endpointsMetadata<World> bld {
        get "/" getHandler (nameof getHandler)
    }
    |> ignore
```

### Route values and JSON content injection

Instead of manually fetching data through `HttpContext`, it is possible to inject them into the handler.

- `httpf` / `uhttpf` injects route values.
  - A single value is injected as is.
  - Multiple values are injected _in order_ as a tuple.
- `httpj` / `uhttpj` injects the deserialized JSON content.
- `httpfj` / `uhttpfj` combines both.

```fsharp
let hello firstname = write $"Hello {firstname}"
let createGift gift = write $"Create a {gift}"
let goodbye (firstname, lastname) gift =
    write $"Goodbye {firstname} {lastname} and here is your {gift}"

let configureEndpoints bld =
    uhttpf  bld Get  "/hello/{firstname}" hello
    uhttpj  bld Post "/gift" createGift
    uhttpfj bld Put  "/goodbye/{firstname}/{lastname}" goodbye
```

## Http Handlers

### Composition

It is possible to combine http handlers.

Those with input injection are railwayed with `Result`.
- `Ok` type can be any value.
- `Error` type is `Map<string, string list>` and the error response is a json of problem details with the status code 422 Unprocessable Entity.

#### Normal with `=>`

```fsharp
let enrich (ctx: HttpContext) =
    ctx.Response.GetTypedHeaders().Set("X-Powered-By", "AspFeat")
    Task.CompletedTask

let configureEndpoints bld =
    uhttp bld Get "/" (enrich => write "hello world")
```

#### Single value injection with `=|`

`Ok` type could be:
  - A route value
  - A tuple of route values
  - A deserialized JSON model
  - Or any mapped value

```fsharp
let validateGetEcho id ctx =
    if id > 0
    then Ok id
    else Map [ ("Id", [ "Is negative or zero" ]) ] |> Error
    |> Task.FromResult

let getEcho id =
    write $"Echo {id}"

let configureEndpoints bld =
    uhttpf bld Get  "/{id:int}" (validateGetEcho =| getEcho)
```

#### Double value injection with `=||`

`Ok` type is a two-value tuple that is then passed to the next function as two parameters.\
It could be:
  1. Fist, route values
  2. Second, deserialized JSON model

```fsharp
let validateCreateEcho id name ctx =
    if not (String.IsNullOrWhiteSpace name)
    then Ok (id, {| Id = id; Name = name |})
    else Map [ ("Name", [ "Is empty" ]) ] |> Error
    |> Task.FromResult

let createEcho id model =
    //...
    createdWith $"/{id}" model

let configureEndpoints bld =
    uhttpfj bld Post "/{id:int}" (validateCreateEcho =|| createEcho)
```