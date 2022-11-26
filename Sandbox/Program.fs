open System.Text.Json
open FSharp.Appsettings
open FSharp.Appsettings.Sandbox.Models

let appsettings = Appsettings.Load()

printfn $"Config: %s{appsettings.ToJsonString(JsonSerializerOptions(WriteIndented = true))}"

let required = appsettings.Deserialize<RequiredConfig>()

printfn $"Required: %A{required}"

appsettings
    .GetPropertyValue("File")
    .GetValue<string>()
|> printfn "Highest priority: %s"

appsettings
    .GetPropertyValue("Secrets")
    .AsObject()
    .GetPropertyValue("ConnectionString")
    .GetValue<string>()
|> printfn "Secret connection string: %s"

appsettings
    .GetPropertyValue("Logging")
    .AsObject()
    .GetPropertyValue("LogLevel")
    .AsObject()
    .GetPropertyValue("Default")
    .GetValue<string>()
|> printfn "Default LogLevel: %s"
