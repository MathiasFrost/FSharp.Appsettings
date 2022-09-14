open System.Text.Json
open FSharp.Appsettings
open FSharp.Appsettings.Sandbox.Models

let appsettings = Appsettings.Load()

printfn "Config: %s" (appsettings.ToJsonString(JsonSerializerOptions(WriteIndented = true)))

let typedAppsettings =
    Appsettings.LoadTyped<Settings>()

printfn "Typed: %A" typedAppsettings
