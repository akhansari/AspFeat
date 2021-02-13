module ReflectSample

open System
open Xunit
open Swensen.Unquote
open AspFeat.Reflect


[<Fact>]
let ``unit`` () = makeSample<unit> () =! ()
[<Fact>]
let ``string`` () = makeSample<string> () =! "string"
[<Fact>]
let ``char`` () = makeSample<char> () =! 'c'
[<Fact>]
let ``bool`` () = makeSample<bool> () =! Unchecked.defaultof<bool>
[<Fact>]
let ``byte`` () = makeSample<byte> () =! Unchecked.defaultof<byte>
[<Fact>]
let ``sbyte`` () = makeSample<sbyte> () =! Unchecked.defaultof<sbyte>
[<Fact>]
let ``int16`` () = makeSample<int16> () =! Unchecked.defaultof<int16>
[<Fact>]
let ``uint16`` () = makeSample<uint16> () =! Unchecked.defaultof<uint16>
[<Fact>]
let ``int32`` () = makeSample<int32> () =! Unchecked.defaultof<int32>
[<Fact>]
let ``uint32`` () = makeSample<uint32> () =! Unchecked.defaultof<uint32>
[<Fact>]
let ``int64`` () = makeSample<int64> () =! Unchecked.defaultof<int64>
[<Fact>]
let ``uint64`` () = makeSample<uint64> () =! Unchecked.defaultof<uint64>
[<Fact>]
let ``decimal`` () = makeSample<decimal> () =! Unchecked.defaultof<decimal>
[<Fact>]
let ``float`` () = makeSample<float> () =! Unchecked.defaultof<float>
[<Fact>]
let ``float32`` () = makeSample<float32> () =! Unchecked.defaultof<float32>

[<Fact>]
let ``Guid`` () = makeSample<Guid> () =! Guid.Empty
[<Fact>]
let ``DateTime`` () = makeSample<DateTime> () =! DateTime.MinValue
[<Fact>]
let ``DateTimeOffset`` () = makeSample<DateTimeOffset> () =! DateTimeOffset.MinValue

[<Fact>]
let ``Option`` () = makeSample<Option<int32>> () =! Some 0
[<Fact>]
let ``Result`` () = makeSample<Result<int32, unit>> () =! Ok 0

[<Fact>]
let ``Tuple`` () = makeSample<(int32 * decimal)> () =! (0, 0m)

type EmptyUnion = EmptyUnion
[<Fact>]
let ``empty Union`` () = makeSample<EmptyUnion> () =! EmptyUnion

type ValueUnion = ValueUnion of int32
[<Fact>]
let ``value Union`` () = makeSample<ValueUnion> () =! ValueUnion 0

type GenericUnion<'T> = GenericUnion of 'T
[<Fact>]
let ``generic Union`` () = makeSample<GenericUnion<int32>> () =! GenericUnion 0

type SimpleRecord = { Id: int32 }
[<Fact>]
let ``simple Record`` () =
    makeSample<SimpleRecord> () =! { Id = 0 }

type GenericRecord<'T> = { Id: 'T; Name: string }
[<Fact>]
let ``generic Record`` () =
    makeSample<GenericRecord<int32>> () =! { Id = 0; Name = "string" }
