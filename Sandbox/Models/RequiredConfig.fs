namespace FSharp.Appsettings.Sandbox.Models

type Secrets = { ConnectionString: string }

type RequiredConfig = { File: string; Secrets: Secrets }
