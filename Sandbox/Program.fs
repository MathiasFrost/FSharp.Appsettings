// For more information see https://aka.ms/fsharp-console-apps

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

type Model = { test: string; children: string [] }
