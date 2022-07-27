namespace FSharp.Appsettings

open System
open System.IO
open FSharp.Data

module Appsettings =

    let private LoadFile filename =
        let exists = File.Exists filename

        match exists with
        | true -> Some(File.ReadAllText filename)
        | false -> None

    /// Find first comma not in quotes
    let rec private FirstComma (str: string) (i: int32) (inQuotes: bool) (escaped: bool): int32 option =
        try
            match str.Chars i with
            | ',' when not inQuotes -> Some i
            | '"' when not escaped -> FirstComma str (i + 1) (not inQuotes) false
            | '"' when escaped -> FirstComma str (i + 1) inQuotes false
            | '\\' when not escaped -> FirstComma str (i + 1) inQuotes true
            | _ -> FirstComma str (i + 1) inQuotes false
        with
            | :? IndexOutOfRangeException -> None
    
    /// Split JSON string on commas not in quotes
    let rec private Split (str: string) (i: int32) (res: string list): string list =
        let firstQuote = str.IndexOf('"', i)
        match FirstComma str firstQuote false false with
        | Some firstComma ->
            let row = str[firstQuote..firstComma - 1]
            printfn "%s" row
            Split str (firstComma + 1) (res @ [row])
        | None -> res
    
    /// Merge two JSON strings
    let private Merge (x: string) (y: string) =
        let rows = Split x (x.IndexOf '{') List.empty
        JsonValue.Parse "{}"

    /// Load appsettings
    let Load: JsonValue =
        let env =
            Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT"

        let envJson =
            match String.IsNullOrWhiteSpace env with
            | true -> None
            | false -> LoadFile $"appsettings.{env}.json"

        match LoadFile "appsettings.json" with
        | Some envRoot when envJson.IsSome -> Merge envRoot envJson.Value // Both merged
        | Some envRoot when envJson.IsNone -> JsonValue.Parse envRoot // Only appsettings.json
        | None when envJson.IsSome -> JsonValue.Parse envJson.Value // Only appsettings.{env}.json
        | _ -> raise (NullReferenceException "No appsettings.json was found") // Both non-existent
