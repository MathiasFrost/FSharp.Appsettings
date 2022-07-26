namespace FSharp.Appsettings

open System
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions

module Appsettings =

    let private LoadFile filename =
        let exists = File.Exists filename

        match exists with
        | true -> Some(File.ReadAllText filename)
        | false -> None

    let private Format key value = $"\"%s{key}\": %s{value}"

    let private Join strings = strings |> String.concat ", "
    
    let rec private Replace (key: string, value: JsonValue) (y: (string * JsonValue)[] option) =
        let yVal =
            match y with
            | Some x -> x |> Array.tryFind (fun (k, _) -> k = key)
            | None -> None

                
        match value with
        | JsonValue.Array x -> Format key $"[{(x |> Seq.map (fun item -> Replace (key, item) yVal) |> Join)}]"
        | JsonValue.Record x -> Format key $"{{ {(x |> Seq.map (fun item -> Replace item x) |> Join)} }}"
        | JsonValue.Null -> Format key "null"
        | x -> Format key $"%A{x}" // ALl other values

    let private Merge x y =
        let jsonX = JsonValue.Parse x
        let jsonY = Some (JsonValue.Parse y).Properties
        let res = jsonX.Properties |> Seq.map (fun x -> Replace x jsonY) |> Join
        
        JsonValue.Parse $"{{ {res} }}"

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
