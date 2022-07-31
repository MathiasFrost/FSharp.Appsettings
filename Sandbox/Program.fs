// For more information see https://aka.ms/fsharp-console-apps

open System.Text.Json
open System.Text.Json.Nodes
open FSharp.Appsettings

type LogLevel =
    { Default: string
      Microsoft: string
      System: string }

type Logging = { LogLevel: LogLevel }
type Settings = { Logging: Logging }

let appsettings = Appsettings.Load()

printfn "Config: %A" appsettings

type Model = { test: string; children: string [] }
