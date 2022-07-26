// For more information see https://aka.ms/fsharp-console-apps

open FSharp.Appsettings
open FSharp.Data
open FSharp.Data.JsonExtensions

type LogLevel =
    { Default: string
      Microsoft: string
      System: string }

type Logging = { LogLevel: LogLevel }
type Settings = { Logging: Logging }

let appsettings = Appsettings.Load

printfn "Config: %A" (appsettings?Logging)

type Model = { test: string; children: string [] }
