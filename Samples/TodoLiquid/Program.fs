
open AspFeat
open AspFeat.Builder
open AspFeat.Endpoint

type Todo =
    { Id: int32
      Name: string
      IsCompleted: bool }

let getTodo =
    { Id = 1; Name = "Go back to work!"; IsCompleted = false }
    |> Liquid.write "Index"

let configureEndpoints bld =
    endpoints bld {
        get "/" getTodo
    }

[<EntryPoint>]
let main args =
    [ Liquid.feat ()
      DefaultResponseCompression.feat ()
      Endpoint.feat configureEndpoints ]
    |> WebApp.run args
