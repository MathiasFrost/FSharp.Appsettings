module MergeRules

open System
open System.Text.Json.Nodes
open System.Text.Json
open NUnit.Framework
open FSharp.Appsettings

[<SetUp>]
let Setup () = Environment.SetEnvironmentVariable("FSHARP_ENVIRONMENT", "Development")

[<Test>]
let ``Highest priority file should be appsettings.Development.local.json`` () =
    let file = appsettings.GetNode "File"
    Assert.That("appsettings.Development.local.json", Is.EqualTo(file.Deserialize<string>()))

[<Test>]
let ``Value B should overwrite Value A`` () =
    let value = appsettings.GetNode "1_ValueOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Value A should be added if not exists on B`` () =
    let value = appsettings.GetNode "2_ValueOnEmpty"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Root")

[<Test>]
let ``Value B should be unchanged if not exists on A`` () =
    let value = appsettings.GetNode "3_EmptyOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Number B should be unchanged if not exists on A`` () =
    let value = appsettings.GetNode "4_EmptyOnNumber"
    Assert.That(value.Deserialize<int32>(), Is.EqualTo 6)

[<Test>]
let ``Object B should be recursively merged`` () =
    let object = appsettings.GetNode "5_ObjectOnObject"
    let value = object.AsObject().GetNode "Prop"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

    let number = object.AsObject().GetNode "Number"
    Assert.That(number.Deserialize<int32>(), Is.EqualTo 6)

[<Test>]
let ``Object A should be added if not exists on B`` () =
    let object = appsettings.GetNode "6_ObjectOnEmpty"
    let value = object.AsObject().GetNode "Prop"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Root")

[<Test>]
let ``Object B should be unchanged if not exists on A`` () =
    let object = appsettings.GetNode "7_EmptyOnObject"
    let value = object.AsObject().GetNode "Prop"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Value B should overwrite Object A`` () =
    let value = appsettings.GetNode "8_ObjectOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Object B should overwrite Value A`` () =
    let object = appsettings.GetNode "9_ValueOnObject"
    let value = object.AsObject().GetNode "Prop"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Object B should overwrite Array A`` () =
    let object = appsettings.GetNode "10_ArrayOnObject"
    let value = object.AsObject().GetNode "Prop"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Value B should overwrite Array A`` () =
    let value = appsettings.GetNode "11_ArrayOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Array B should overwrite Value A`` () =
    let array = appsettings.GetNode "12_ValueOnArray"
    let value = array.AsArray().Item 0
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Array B should exclusive merge with Array A`` () =
    let array = appsettings.GetNode "13_ArrayOnArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Dev\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Root\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "6"))

[<Test>]
let ``Array B should exclusive merge with 2D Array A`` () =
    let array = appsettings.GetNode "14_2DArrayOnArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Dev\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[\"Root\"]"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "6"))

[<Test>]
let ``2D Array B should exclusive merge with 2D Array A`` () =
    let array = appsettings.GetNode "15_2DArrayOn2DArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[\"Dev\"]"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[\"Root\"]"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[6,6]"))

[<Test>]
let ``Array B should exclusive merge with Object Array A`` () =
    let array = appsettings.GetNode "16_ObjectArrayOnArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Dev\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Prop\":\"Root\"}"))

[<Test>]
let ``Object Array B should exclusive merge with Object Array A`` () =
    let array = appsettings.GetNode "17_ObjectArrayOnObjectArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Prop\":\"Dev\"}"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Prop\":\"Root\"}"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Bool\":true}"))
