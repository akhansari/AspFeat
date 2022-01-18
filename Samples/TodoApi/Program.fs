module TodoApi.Program

open System
open AspFeat
open AspFeat.Builder
open AspFeat.Endpoint
open AspFeat.HttpContext
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http

type TodoAddModel =
    { Todo: string }

[<NoComparison>]
type TodoUpdateModel =
    { Todo: string
      Completed: Nullable<bool> }

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
    match db.TryGet id with
    | Some todo -> writeAsJson todo ctx
    | None      -> notFound ctx

let validateAddTodo (model: TodoAddModel) _ =
    task {
        if String.IsNullOrWhiteSpace model.Todo |> not
        then return { Todo = model.Todo; Completed = false } |> Ok
        else return Map [ ("todo", [ "Is empty" ]) ] |> Error
    }

let addTodo todo ctx =
    match db.TryAdd todo with
    | Some (id, todo) -> createdWith $"/{id}" todo ctx
    | None            -> conflict ctx

let updateTodo id (model: TodoUpdateModel) ctx =
    let mergeWith old : Todo =
        match (Option.ofObj model.Todo, Option.ofNullable model.Completed) with
        | Some todo, Some completed -> { old with Todo = todo; Completed = completed }
        | Some todo, None           -> { old with Todo = todo }
        | None, Some completed      -> { old with Completed = completed }
        | None, None                ->   old
    match db.TryGet id with
    | Some old ->
        match db.TryUpdate id old (mergeWith old) with
        | Some todo -> writeAsJson todo ctx
        | None      -> conflict ctx
    | None -> notFound ctx

let deleteTodo id ctx =
    match db.TryRemove id with
    | Some _ -> noContent ctx
    | None   -> notFound ctx

type Todos =
    abstract member GetTodos: unit -> Map<int32, Todo>

    [<ProducesResponseType(typeof<Todo>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    abstract member GetTodo: id: int32 -> unit

    [<ProducesResponseType(StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status409Conflict)>]
    abstract member AddTodo: model: TodoAddModel -> unit

    [<ProducesResponseType(typeof<Todo>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status409Conflict)>]
    abstract member UpdateTodo: id: int32 -> model: TodoUpdateModel -> unit

    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    abstract member DeleteTodo: id: int32 -> unit

let configureEndpoints bld =
    endpointsMetadata<Todos> bld {
        get    "/todos"            getTodos   (nameof getTodos)
        get    "/todos/{id:int}"   getTodo    (nameof getTodo)
        post   "/todos"            (validateAddTodo =| addTodo) (nameof addTodo)
        put    "/todos/{id:int}"   updateTodo (nameof updateTodo)
        delete "/todos/{id:int}"   deleteTodo (nameof deleteTodo)
    }

[<EntryPoint>]
let main args =
    [ Endpoint.feat configureEndpoints
      Swagger.feat () ]
    |> WebApp.run args
