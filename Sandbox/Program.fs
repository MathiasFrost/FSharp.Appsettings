open System.Text.Json
open FSharp.Appsettings

type LogLevel =
    { Default: string
      Microsoft: string
      System: string }

type Logging = { LogLevel: LogLevel }
type Settings = { Logging: Logging }

let appsettings = Appsettings.Load()

printfn "Config: %s" (appsettings.ToJsonString(JsonSerializerOptions(WriteIndented = true)))

let typedAppsettings =
    Appsettings.LoadTyped<Settings>()

printfn "Typed: %A" typedAppsettings
