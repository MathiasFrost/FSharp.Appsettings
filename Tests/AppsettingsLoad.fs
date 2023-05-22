module AppsettingsLoad

open System
open System.IO
open System.Text.Json
open NUnit.Framework
open FSharp.Appsettings

[<SetUp>]
let Setup () = Environment.SetEnvironmentVariable("FSHARP_ENVIRONMENT", "Development")

[<Test>]
let ``FSHARP_ENVIRONMENT should be Development`` () =
    let env =
        try
            Some(Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT")
        with
        | :? ArgumentNullException -> None

    Assert.That(env.IsSome, Is.True)
    Assert.That(env.Value, Is.EqualTo "Development")

[<Test>]
let ``Result should be empty if no appsettings.json files`` () =
    let rootJson = File.ReadAllText "appsettings.json"
    let envJson = File.ReadAllText "appsettings.Development.json"
    let rootLocalJson = File.ReadAllText "appsettings.local.json"
    let envLocalJson = File.ReadAllText "appsettings.Development.local.json"

    File.Delete "appsettings.json"
    File.Delete "appsettings.Development.json"
    File.Delete "appsettings.local.json"
    File.Delete "appsettings.Development.local.json"

    let appsettings = LoadAppsettings()

    // Restore files
    File.WriteAllText("appsettings.json", rootJson)
    File.WriteAllText("appsettings.Development.json", envJson)
    File.WriteAllText("appsettings.local.json", rootLocalJson)
    File.WriteAllText("appsettings.Development.local.json", envLocalJson)

    Assert.That("{}", Is.EqualTo(appsettings.ToJsonString()))

[<Test>]
let ``Highest priority should be appsettings.json when no higher exists`` () =
    let envJson = File.ReadAllText "appsettings.Development.json"
    let rootLocalJson = File.ReadAllText "appsettings.local.json"
    let envLocalJson = File.ReadAllText "appsettings.Development.local.json"

    File.Delete "appsettings.Development.json"
    File.Delete "appsettings.local.json"
    File.Delete "appsettings.Development.local.json"

    let appsettings = LoadAppsettings()

    // Restore files
    File.WriteAllText("appsettings.Development.json", envJson)
    File.WriteAllText("appsettings.local.json", rootLocalJson)
    File.WriteAllText("appsettings.Development.local.json", envLocalJson)

    let file = appsettings.GetNode "File"
    Assert.That("appsettings.json", Is.EqualTo(file.Deserialize<string>()))

[<Test>]
let ``Highest priority should be appsettings.Development.json when no higher exists`` () =
    let rootLocalJson = File.ReadAllText "appsettings.local.json"
    let envLocalJson = File.ReadAllText "appsettings.Development.local.json"

    File.Delete "appsettings.local.json"
    File.Delete "appsettings.Development.local.json"

    let appsettings = LoadAppsettings()

    // Restore files
    File.WriteAllText("appsettings.local.json", rootLocalJson)
    File.WriteAllText("appsettings.Development.local.json", envLocalJson)

    let file = appsettings.GetNode "File"
    Assert.That("appsettings.Development.json", Is.EqualTo(file.Deserialize<string>()))

[<Test>]
let ``Highest priority should be appsettings.local.json when no higher exists`` () =
    let envLocalJson = File.ReadAllText "appsettings.Development.local.json"

    File.Delete "appsettings.Development.local.json"

    let appsettings = LoadAppsettings()

    // Restore files
    File.WriteAllText("appsettings.Development.local.json", envLocalJson)

    let file = appsettings.GetNode "File"
    Assert.That("appsettings.local.json", Is.EqualTo(file.Deserialize<string>()))

[<Test>]
let ``Highest priority should be appsettings.Development.local.json`` () =
    let appsettings = LoadAppsettings()
    let file = appsettings.GetNode "File"
    Assert.That("appsettings.Development.local.json", Is.EqualTo(file.Deserialize<string>()))

[<Test>]
let ``Highest priority should be appsettings.local.json if FSHARP_ENVIRONMENT not set`` () =
    Environment.SetEnvironmentVariable("FSHARP_ENVIRONMENT", null)

    let appsettings = LoadAppsettings()
    let file = appsettings.GetNode "File"
    Assert.That("appsettings.local.json", Is.EqualTo(file.Deserialize<string>()))
