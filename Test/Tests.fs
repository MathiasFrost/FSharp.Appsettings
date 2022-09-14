module Tests

open FSharp.Appsettings.Sandbox.Models
open Xunit
open FSharp.Appsettings

[<Fact>]
let ``Can load appsettings`` () =
    let appsettings = Appsettings.Load()
    Assert.NotEmpty(appsettings.ToJsonString())

[<Fact>]
let ``Can load typed appsettings`` () =
    let appsettings =
        Appsettings.LoadTyped<Settings>()

    Assert.IsAssignableFrom<Settings>(appsettings)
