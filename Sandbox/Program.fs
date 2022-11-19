open System
open System.Text.Json
open FSharp.Appsettings
open FSharp.Data

type LogLevel =
    { Default: string
      Microsoft: string
      System: string }

type Logging = { LogLevel: LogLevel }
type Settings = { Logging: Logging; BaseUrl: string; }

let appsettings = Appsettings.Load()

printfn "Config: %s" (appsettings.ToJsonString(JsonSerializerOptions(WriteIndented = true)))

let typedAppsettings =
    Appsettings.LoadTyped<Settings>()

printfn "Typed: %A" typedAppsettings

type WeatherForecast =
    { date: DateTime
      temperatureC: int32
      temperatureF: int32
      summary: Option<string> }

let GetFromJson<'T> () =
    let response =
        Http.RequestString (Uri(Uri(typedAppsettings.BaseUrl), "/WeatherForecast").ToString())

    JsonSerializer.Deserialize<'T>(response)


GetFromJson<seq<WeatherForecast>>()
|> Seq.iter (fun x -> printfn "%b" (x.summary.IsSome))
