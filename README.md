# AspFeat

A modular low ceremony toolkit for ASP.Net Core and F#.

- Modular injection of services and middlewares.
- Set of low ceremony ready-to-use setups.
- Only functional helpers over ASP.Net Core, nothing else.
- Focus on Web APIs.

## Startup

Usually in order to setup a feature properly, it's necessary to first add the services to `IServiceCollection` and then use the middlewares with `IApplicationBuilder`. The downside is that they are mixed with other features and moreover the order of calls are important, which makes everything complicated.

To keep the startup clean, the idea is to package features into modules and then expose the service collection and the application builder as a tuple.

The end result is that ASP.Net core startup has never been so easy:

```f#
[<EntryPoint>]
let main _ =
    let configure bld = http bld Get "/" (write "hello world")
    WebHost.run [ Endpoint.feat configure ]
```

Implementation behind:

```f#
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

AspFeat brings several helpers in order to facilitate the use with a more functional paradigm.\
The idea is not to change completely the way of using but only by bringing a parallel addition.\
So you are not limited by AspFeat and if needed you can use ASP.Net as usual.

Default usage:
```f#
let configureEndpoints (bld: IEndpointRouteBuilder) =
    bld.MapGet("/", RequestDelegate getHandler) |> ignore
```

With AspFeat toolkit:
```f#
let configureEndpoints bld =
    http bld Get "/" getHandler
```

### Route values and JSON content injection

Instead of getting data manually via `HttpContext`, it's possible to inject them into the handler.

- `httpf` injects route parameter values.
  - A single value is injected as is.
  - Multiple values are injected _in order_ as a tuple.
- `httpj` injects the deserialized JSON content.
- `httpfj` combines both.

```f#
let hello firstname = write $"Hello {firstname}"
let goodbye (firstname, lastname) = write $"Goodbye {firstname} {lastname}"

let configureEndpoints (bld: IEndpointRouteBuilder) =
    httpf bld Get "hellp/{firstname}" hello
    httpf bld Get "goodbye/{firstname}/{lastname}" goodbye
```

You can find more examples is the samples folder.
