namespace FSharp.Appsettings.Sandbox.Models

type CORS = { AllowedOrigins: string list }
type Secrets = { ConnectionString: string }

type RequiredConfig = { Env: string; CORS: CORS; OnlyDev: string }
