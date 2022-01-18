module Tests.Reflect

open System
open FsCheck
open FsCheck.Xunit
open Swensen.Unquote
open AspFeat.Reflect

[<Property>]
let ``should get int32 inject value`` echo =
    createTupleMaker<int32> [|"id"|] (fun _ -> string echo) =! echo

[<Property>]
let ``should get int64 inject value`` echo =
    createTupleMaker<int64> [|"id"|] (fun _ -> string echo) =! echo

[<Property>]
let ``should get decimal inject value`` echo =
    createTupleMaker<decimal> [|"id"|] (fun _ -> string echo) =! echo

[<Property>]
let ``should get float inject value`` (NormalFloat echo) =
    createTupleMaker<float> [|"id"|] (fun _ -> string echo) =! echo

[<Property>]
let ``should get float32 inject value`` echo =
    not (Single.IsNaN echo) ==> lazy
    createTupleMaker<float32> [|"id"|] (fun _ -> string echo) =! echo

[<Property>]
let ``should get bool inject value`` echo =
    createTupleMaker<bool> [|"id"|] (fun _ -> string echo) =! echo

[<Property>]
let ``should get date time inject value`` (echo: DateTime) =
    createTupleMaker<DateTime> [|"id"|] (fun _ -> echo.ToString "s")
    =! echo.AddMilliseconds (float -echo.Millisecond)

[<Property>]
let ``should get date time offset inject value`` (echo: DateTimeOffset) =
    createTupleMaker<DateTimeOffset> [|"id"|] (fun _ -> echo.ToString "o") =! echo

[<Property>]
let ``should get guid inject value`` echo =
    createTupleMaker<Guid> [|"id"|] (fun _ -> string echo) =! echo

[<Property>]
let ``should get tuple2 inject value`` echo1 echo2 =
    let m = Map [ ("p1", box echo1); ("p2", box echo2) ]
    createTupleMaker<Guid * int32> [|"p1";"p2"|] (fun name -> string m.[name]) =! (echo1, echo2)
