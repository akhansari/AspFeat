namespace TodoApi

open System.Collections.Concurrent
open System.Threading

type ValueStore<'T> () =

    let db = ConcurrentDictionary<int32, 'T>()
    let counter = ref 0

    member _.TryGet id =
        match db.TryGetValue id with
        | true, value -> Some value
        | _ -> None

    member _.GetAll () =
        db |> Seq.map (|KeyValue|) |> Map.ofSeq
    member _.Values with get () =
        db.Values |> List.ofSeq

    member _.TryAdd value =
        let id = Interlocked.Increment counter
        if db.TryAdd (id, value)
        then Some (id, value)
        else None

    member _.TryUpdate id oldValue value =
        if db.TryUpdate (id, value, oldValue)
        then Some value else None

    member _.TryRemove id =
        let (removed, value) = db.TryRemove id
        if removed then Some value else None
