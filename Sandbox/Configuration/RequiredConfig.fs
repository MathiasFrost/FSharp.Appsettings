namespace FSharp.Appsettings.Sandbox.Configuration

type Secrets = { ConnectionString: string }

type RequiredConfig = { File: string; Secrets: Secrets }
