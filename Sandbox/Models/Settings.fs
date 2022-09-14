namespace FSharp.Appsettings.Sandbox.Models

type LogLevel =
    { Default: string
      Microsoft: string
      System: string }

type Logging = { LogLevel: LogLevel }

type Settings = { Logging: Logging }
