open System.Text.Json
open FSharp.Appsettings
open Microsoft.Extensions.Logging

printfn $"Config: %s{appsettings.ToJsonString(JsonSerializerOptions(WriteIndented = true))}"

appsettings |> value<string> "File" |> printfn "Highest priority: %s"

appsettings |> object "Secrets" |> value<string> "ConnectionString" |> printfn "Secret connection string: %s"

appsettings |> object "Logging" |> object "LogLevel" |> value<string> "Default" |> printfn "Default LogLevel: %s"

appsettings |> array "Arr" |> iteri (fun i node -> node |> value<string> "Something" |> printfn "Something: %d %s" i)
appsettings |> array "Arr" |> list |> List.iteri (fun i node -> node |> value<string> "Something" |> printfn "Something: %d %s" i)

type Program = { unit: unit }

let logger =
    LoggerFactory
        .Create(fun builder -> builder |> ignore)
        .CreateLogger<Program>()

let logInformation (message: string) =
    logger.LogInformation message
    printfn $"Information: %s{message}"

logInformation "Hello"
