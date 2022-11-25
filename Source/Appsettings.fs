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
        let env =
            try
                Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT"
            with
            | :? ArgumentNullException -> null

        let rootJson =
            TryReadFile "appsettings.json"

        let envJson =
            match env = null with
            | true -> None
            | false -> TryReadFile $"appsettings.{env}.json"

        let localRootJson =
            TryReadFile "appsettings.local.json"

        let localEnvJson =
            match env = null with
            | true -> None
            | false -> TryReadFile $"appsettings.{env}.local.json"

        let mergedToEnv =
            match rootJson, envJson with
            | Some root, Some env -> Merge (ToObject root) (ToObject env) // Both merged
            | Some root, None -> ToObject root // Only appsettings.json
            | None, Some env -> ToObject env // Only appsettings.{FSHARP_ENVIRONMENT}.json
            | None, None -> JsonObject.Create(JsonElement())

        let mergedToLocal =
            match localRootJson with
            | Some localRoot -> Merge mergedToEnv (ToObject localRoot) // Both merged
            | None -> mergedToEnv // Only root and env

        match localEnvJson with
        | Some localEnv -> Merge mergedToLocal (ToObject localEnv) // Both merged
        | None -> mergedToLocal // Only root, env and local
