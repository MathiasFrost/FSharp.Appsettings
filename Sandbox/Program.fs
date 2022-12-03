open System.Text.Json
open FSharp.Appsettings
open FSharp.Appsettings.Sandbox.Configuration
open Microsoft.Extensions.Logging

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

type Program = { unit: unit }

let logger =
    LoggerFactory
        .Create(fun builder -> builder |> ignore)
        .CreateLogger<Program>()

let logInformation (message: string) =
    logger.LogInformation message
    printfn $"Information: %s{message}"

logInformation "Hello"
