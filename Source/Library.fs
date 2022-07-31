namespace FSharp.Appsettings

open System
open System.IO
open System.Text.Json.Nodes

module Appsettings =

    /// Read file if exists
    let private TryReadFile filename =
        let exists = File.Exists filename

        match exists with
        | true -> Some(File.ReadAllText filename)
        | false -> None

    // Find out which value type the JsonNode is
    let (|Value|Object|Array|) (node: JsonNode) =
        try
            node.AsValue() |> ignore
            Value
        with
        | :? InvalidOperationException ->
            try
                node.AsObject() |> ignore
                Object
            with
            | :? InvalidOperationException ->
                try
                    node.AsArray() |> ignore
                    Array
                with
                | :? InvalidOperationException -> failwith "JsonNode could not be cast to anything"

    // Try to find the value of a key
    let private TryFind (key: string) (env: JsonObject) : JsonNode option =
        let item = env.Item key
        if item = null then None else Some item

    /// Add thee root values that does not exist in env
    let rec private Merge (root: JsonObject) (env: JsonObject) : JsonObject =
        let i = root.GetEnumerator()

        while i.MoveNext() do
            match i.Current.Value with
            | Value ->
                match TryFind i.Current.Key env with
                | Some _ -> ()
                | None -> env.Add(i.Current.Key, i.Current.Value.ToString())
            | Object ->
                match TryFind i.Current.Key env with
                | Some x ->
                    Merge (i.Current.Value.AsObject()) (x.AsObject())
                    |> ignore
                | None -> ()
            | Array -> failwith "TOOD: arrays"

        env

    /// Turn a JSON string into a JsonObject
    let private ToObject (str: string) = (JsonNode.Parse str).AsObject()

    /// Load appsettings
    let Load () : JsonObject =
        let env =
            Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT"

        let envJson =
            match String.IsNullOrWhiteSpace env with
            | true -> None
            | false -> TryReadFile $"appsettings.{env}.json"

        match TryReadFile "appsettings.json" with
        | Some rootEnv when envJson.IsSome ->
            (ToObject rootEnv, ToObject envJson.Value)
            ||> Merge // Both merged
        | Some rootEnv when envJson.IsNone -> ToObject rootEnv // Only appsettings.json
        | None when envJson.IsSome -> ToObject envJson.Value // Only appsettings.{env}.json
        | _ -> raise (NullReferenceException "No appsettings.json was found") // Both non-existent
