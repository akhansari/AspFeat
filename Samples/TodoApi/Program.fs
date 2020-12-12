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

let getTodo id ctx =
    db.TryGet id
    |> Option.map (writeAsJsonTo ctx)
    |> Option.defaultWith (fun () -> notFound ctx)

let addTodo (model: {| Todo: string |}) ctx =
    db.TryAdd ({ Todo = model.Todo; Completed = false })
    |> Option.map (fun (id, todo) -> createdWith ctx $"/{id}" todo)
    |> Option.defaultWith (fun () -> conflict ctx)

let updateTodo id ctx =
    let update old todo completed =
        match (todo, completed) with
        | Some todo, Some completed -> { old with Todo = todo; Completed = completed }
        | Some todo, None           -> { old with Todo = todo }
        | None, Some completed      -> { old with Completed = completed }
        | None, None                ->   old
    unitTask {
        let! model = readAsJson<{| Todo: string option; Completed: bool option |}> ctx
        do! db.TryGet id
            |> Option.map (fun old ->
                update old model.Todo model.Completed
                |> db.TryUpdate id old
                |> Option.map (writeAsJsonTo ctx)
                |> Option.defaultWith (fun () -> conflict ctx))
            |> Option.defaultWith (fun () -> notFound ctx)
    }

let deleteTodo id ctx =
    db.TryRemove id
    |> Option.map (fun _ -> noContent ctx)
    |> Option.defaultWith (fun () -> notFound ctx)

let configureEndpoints bld =
    http  bld Get    "/todos"          getTodos
    httpf bld Get    "/todos/{id:int}" getTodo
    httpf bld Put    "/todos/{id:int}" updateTodo
    httpj bld Post   "/todos"          addTodo
    httpf bld Delete "/todos/{id:int}" deleteTodo

[<EntryPoint>]
let main _ =
    WebHost.run [ Endpoint.feat configureEndpoints ]
