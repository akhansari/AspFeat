open NBomber
open NBomber.Contracts
open NBomber.FSharp
open NBomber.Plugins.Http.FSharp

let clientFactory = HttpClientFactory.create ()

let aspFeatStep =
    Step.create(
        "aspfeat",
        clientFactory = clientFactory,
        execute = fun context ->
            Http.createRequest "GET" "http://localhost:5000/hello"
            |> Http.send context
    )

let giraffeStep =
    Step.create(
        "giraffe",
        clientFactory = clientFactory,
        execute = fun context ->
            Http.createRequest "GET" "http://localhost:5111/hello"
            |> Http.send context
    )

Scenario.create "simple_webapi" [ aspFeatStep; giraffeStep ]
|> Scenario.withWarmUpDuration(seconds 5)
|> Scenario.withLoadSimulations [ RampPerSec(rate = 100, during = seconds 30) ]
|> NBomberRunner.registerScenario
|> NBomberRunner.withTestSuite "webapi_bench"
|> NBomberRunner.run
|> ignore

stdout.WriteLine ". . ."
stdin.Read() |> ignore
