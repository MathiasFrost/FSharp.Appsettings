namespace FSharp.Appsettings

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes

module Appsettings =

    /// Read file if exists
    let private TryReadFile filename =
        let exists = File.Exists filename

        match exists with
        | true -> Some(File.ReadAllText filename)
        | false -> None

    /// Find out which value type the JsonNode is
    let private (|Value|Object|Array|) (node: JsonNode) =
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

    /// Try to find the value of a key
    let private TryFind (key: string) (env: JsonObject) : JsonNode option =
        let item = env.Item key
        if item = null then None else Some item

    /// Check if JSON array contains a value
    let private Exists (arr: JsonArray) (value: string) : bool =
        arr |> Seq.exists (fun x -> x.ToString() = value)

    /// Add array elements from root that does not exist in env
    let rec private MergeArray (arr: JsonArray) (env: JsonNode) : unit =
        let i = arr.GetEnumerator()

        while i.MoveNext() do
            match i.Current with
            | Value ->
                match env with
                | Value -> failwith "Attempted to add value to a value type" // Adding value to Value (Not possible)
                | Object -> failwith "Adding value to object not supported" // Adding Value to an object ("{i}": <value>?)
                | Array -> // Adding Value to array (normal array merging)
                    if not (Exists (env.AsArray()) (i.Current.ToString())) then
                        env.AsArray().Add(i.Current.ToString())
            | Object ->
                match env with
                | Value -> failwith "Attempted to add object to a value type" // Adding Object to Value (Not possible)
                | Object -> failwith "Adding object to object not supported" // Adding Object to Object ("{i}": <object>?)
                | Array -> failwith "Adding object to array not supported" // Adding Object to Array (recursive array merging)
            | Array ->
                match env with
                | Value -> failwith "Attempted to add array to a value type" // Adding Array to Value (Not possible)
                | Object -> failwith "Adding array to object not supported" // Adding Array to Object ("{i}": <array>?)
                | Array -> MergeArray (i.Current.AsArray()) env // Adding Array to Array (recursive array merging)

    /// Add thee root values that does not exist in env
    and private Merge (root: JsonObject) (env: JsonObject) : JsonObject =
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
                | None -> env.Add(i.Current.Key, JsonNode.Parse(i.Current.Value.ToJsonString()))
            | Array ->
                match TryFind i.Current.Key env with
                | Some x -> MergeArray (i.Current.Value.AsArray()) x
                | None -> env.Add(i.Current.Key, JsonNode.Parse(i.Current.Value.ToJsonString()))

        env

    /// Turn a JSON string into a JsonObject
    let private ToObject (str: string) = (JsonNode.Parse str).AsObject()

    /// Load appsettings
    let Load () : JsonObject =
        let env = Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT"

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

    /// Load appsettings
    let LoadTyped<'T> () : 'T = Load().Deserialize<'T>()
