module WebApi.Program

open System
open FSharp.Control.Tasks
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext

type Todo =
    { Todo: string
      Completed: bool }

let db = ValueStore<Todo> ()

let getTodos ctx =
    db.GetAll ()
    |> match QueryString.tryFindOne ctx "completed" with
       | Some completed -> Map.filter (fun _ todo -> todo.Completed = Convert.ToBoolean completed)
       | None -> id
    |> writeAsJsonTo ctx

let getTodo ctx =
    RouteValue.find ctx "id" |> int32
    |> db.TryGet
    |> Option.map (writeAsJsonTo ctx)
    |> Option.defaultWith (fun () -> notFound ctx)

let addTodo ctx =
    unitTask {
        let! model = readAsJson<{| Todo: string |}> ctx
        do! db.TryAdd ({ Todo = model.Todo; Completed = false })
            |> Option.map (fun (id, todo) -> createdWith ctx $"/{id}" todo)
            |> Option.defaultWith (fun () -> conflict ctx)
    }

let updateTodo ctx =
    let update old todo completed =
        match (todo, completed) with
        | Some todo, Some completed -> { old with Todo = todo; Completed = completed }
        | Some todo, None           -> { old with Todo = todo }
        | None, Some completed      -> { old with Completed = completed }
        | None, None                ->   old
    unitTask {
        let id = RouteValue.find ctx "id" |> int32
        let! model = readAsJson<{| Todo: string option; Completed: bool option |}> ctx
        do! db.TryGet id
            |> Option.map (fun old ->
                update old model.Todo model.Completed
                |> db.TryUpdate id old
                |> Option.map (writeAsJsonTo ctx)
                |> Option.defaultWith (fun () -> conflict ctx))
            |> Option.defaultWith (fun () -> notFound ctx)
    }

let deleteTodo ctx =
    RouteValue.find ctx "id" |> int32
    |> db.TryRemove
    |> Option.map (fun _ -> noContent ctx)
    |> Option.defaultWith (fun () -> notFound ctx)

let configureEndpoints bld =
    let http = http bld
    http Get    "/todos"          getTodos
    http Get    "/todos/{id:int}" getTodo
    http Put    "/todos/{id:int}" updateTodo
    http Post   "/todos"          addTodo
    http Delete "/todos/{id:int}" deleteTodo

[<EntryPoint>]
let main _ =
    WebHost.run [ Endpoint.feat configureEndpoints ]
