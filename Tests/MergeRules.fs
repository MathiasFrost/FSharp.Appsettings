module MergeRules

open System
open System.IO
open System.Text.Json.Nodes
open System.Text.Json
open NUnit.Framework
open FSharp.Appsettings

[<SetUp>]
let Setup () = Environment.SetEnvironmentVariable("FSHARP_ENVIRONMENT", "Development")

[<Test>]
let ``Highest priority file should be appsettings.Development.local.json`` () =
    let appsettings = Appsettings.Load()
    let file = appsettings.GetPropertyValue "File"
    Assert.That("appsettings.Development.local.json", Is.EqualTo(file.Deserialize<string>()))

[<Test>]
let ``Value B should overwrite Value A`` () =
    let appsettings = Appsettings.Load()
    let value = appsettings.GetPropertyValue "1_ValueOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Value A should be added if not exists on B`` () =
    let appsettings = Appsettings.Load()
    let value = appsettings.GetPropertyValue "2_ValueOnEmpty"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Root")

[<Test>]
let ``Value B should be unchanged if not exists on A`` () =
    let appsettings = Appsettings.Load()
    let value = appsettings.GetPropertyValue "3_EmptyOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Number B should be unchanged if not exists on A`` () =
    let appsettings = Appsettings.Load()
    let value = appsettings.GetPropertyValue "4_EmptyOnNumber"
    Assert.That(value.Deserialize<int32>(), Is.EqualTo 6)

[<Test>]
let ``Object B should be recursively merged`` () =
    let appsettings = Appsettings.Load()
    let object = appsettings.GetPropertyValue "5_ObjectOnObject"
    let value = object.AsObject().GetPropertyValue "Field"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

    let number = object.AsObject().GetPropertyValue "Number"
    Assert.That(number.Deserialize<int32>(), Is.EqualTo 6)

[<Test>]
let ``Object A should be added if not exists on B`` () =
    let appsettings = Appsettings.Load()
    let object = appsettings.GetPropertyValue "6_ObjectOnEmpty"
    let value = object.AsObject().GetPropertyValue "Field"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Root")

[<Test>]
let ``Object B should be unchanged if not exists on A`` () =
    let appsettings = Appsettings.Load()
    let object = appsettings.GetPropertyValue "7_EmptyOnObject"
    let value = object.AsObject().GetPropertyValue "Field"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Value B should overwrite Object A`` () =
    let appsettings = Appsettings.Load()
    let value = appsettings.GetPropertyValue "8_ObjectOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Object B should overwrite Value A`` () =
    let appsettings = Appsettings.Load()
    let object = appsettings.GetPropertyValue "9_ValueOnObject"
    let value = object.AsObject().GetPropertyValue "Field"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Object B should overwrite Array A`` () =
    let appsettings = Appsettings.Load()
    let object = appsettings.GetPropertyValue "10_ArrayOnObject"
    let value = object.AsObject().GetPropertyValue "Field"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Value B should overwrite Array A`` () =
    let appsettings = Appsettings.Load()
    let value = appsettings.GetPropertyValue "11_ArrayOnValue"
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Array B should overwrite Value A`` () =
    let appsettings = Appsettings.Load()
    let array = appsettings.GetPropertyValue "12_ValueOnArray"
    let value = array.AsArray().Item 0
    Assert.That(value.Deserialize<string>(), Is.EqualTo "Dev")

[<Test>]
let ``Array B should exclusive merge with Array A`` () =
    let appsettings = Appsettings.Load()
    let array = appsettings.GetPropertyValue "13_ArrayOnArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Dev\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Root\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "6"))

[<Test>]
let ``Array B should exclusive merge with 2D Array A`` () =
    let appsettings = Appsettings.Load()
    let array = appsettings.GetPropertyValue "14_2DArrayOnArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Dev\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[\"Root\"]"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "6"))

[<Test>]
let ``2D Array B should exclusive merge with 2D Array A`` () =
    let appsettings = Appsettings.Load()
    let array = appsettings.GetPropertyValue "15_2DArrayOn2DArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[\"Dev\"]"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[\"Root\"]"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "[6,6]"))

[<Test>]
let ``Array B should exclusive merge with Object Array A`` () =
    let appsettings = Appsettings.Load()
    let array = appsettings.GetPropertyValue "16_ObjectArrayOnArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "\"Dev\""))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Field\":\"Root\"}"))

[<Test>]
let ``Object Array B should exclusive merge with Object Array A`` () =
    let appsettings = Appsettings.Load()
    let array = appsettings.GetPropertyValue "17_ObjectArrayOnObjectArray"
    let values = array.Deserialize<JsonNode list>()
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Field\":\"Dev\"}"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Field\":\"Root\"}"))
    Assert.True(values |> List.exists (fun x -> x.ToJsonString() = "{\"Bool\":true}"))
