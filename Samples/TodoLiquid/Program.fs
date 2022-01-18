open AspFeat
open AspFeat.Builder
open AspFeat.Endpoint

type Todo =
    { Id: int32
      Name: string
      IsComplete: bool }

let getTodo =
    { Id = 1; Name = "Go back to work!"; IsComplete = false }
    |> Liquid.write "Index"

let configureEndpoints bld =
    endpoints bld {
        get "/" getTodo
    }

[<EntryPoint>]
let main args =
    [ Liquid.feat ()
      ResponseCompression.feat Feat.ignore
      Endpoint.feat configureEndpoints ]
    |> WebApp.run args
