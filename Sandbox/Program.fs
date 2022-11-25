open System.Text.Json
open FSharp.Appsettings
open FSharp.Appsettings.Sandbox.Models

let appsettings = Appsettings.Load()

printfn $"Config: %s{appsettings.ToJsonString(JsonSerializerOptions(WriteIndented = true))}"

match appsettings.TryGetPropertyValue "Env" with
| true, x -> printfn $"Env: %A{x}"
| false, _ -> failwith "Could not get Env"

match appsettings.TryGetPropertyValue "Secrets" with
| true, x ->
    match x.AsObject().TryGetPropertyValue "ConnectionString" with
    | true, y -> printfn $"Secret connection string: %A{y}"
    | false, _ -> failwith "Could not get Secrets__ConnectionString"
| false, _ -> failwith "Could not get Secrets"

match appsettings.TryGetPropertyValue "Logging" with
| true, x ->
    match x.AsObject().TryGetPropertyValue "LogLevel" with
    | true, y ->
        match y.AsObject().TryGetPropertyValue "Default" with
        | true, z -> printfn $"Default LogLevel: %A{z}"
        | false, _ -> failwith "Could not get Logging__LogLevel__Default"
    | false, _ -> failwith "Could not get Logging__LogLevel"
| false, _ -> failwith "Could not get Logging"
