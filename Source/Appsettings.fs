namespace FSharp.Appsettings

open System
open System.IO
open System.Text.Json.Nodes

module Appsettings =

    /// Returns an empty "{}" JsonObject
    let private EmptyJsonObject () = JsonObject.Parse("{}").AsObject()

    /// Read file and return a JsonObject. If it doesn't exist return en empty JsonObject
    let private ParseJsonFile filename =
        match File.Exists filename with
        | true ->
            (File.ReadAllText filename |> JsonObject.Parse)
                .AsObject()
        | false -> EmptyJsonObject()

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
    let private Exists (arr: JsonArray) (value: string) : bool = arr |> Seq.exists (fun x -> x.ToString() = value)

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
                    if not (Exists (env.AsArray()) (i.Current.ToString())) then env.AsArray().Add(i.Current.ToString())
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
                | Some x -> Merge (i.Current.Value.AsObject()) (x.AsObject()) |> ignore
                | None -> env.Add(i.Current.Key, JsonNode.Parse(i.Current.Value.ToJsonString()))
            | Array ->
                match TryFind i.Current.Key env with
                | Some x -> MergeArray (i.Current.Value.AsArray()) x
                | None -> env.Add(i.Current.Key, JsonNode.Parse(i.Current.Value.ToJsonString()))

        env

    /// Load appsettings
    let Load () : JsonObject =
        let fsEnv =
            try
                Some(Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT")
            with
            | :? ArgumentNullException -> None

        let rootJson = ParseJsonFile "appsettings.json"

        let envJson =
            match fsEnv with
            | Some env -> ParseJsonFile $"appsettings.{env}.json"
            | None -> EmptyJsonObject()

        let localRootJson = ParseJsonFile "appsettings.local.json"

        let localEnvJson =
            match fsEnv with
            | Some env -> ParseJsonFile $"appsettings.{env}.local.json"
            | None -> EmptyJsonObject()

        let mergedToEnv = Merge rootJson envJson
        let mergedToLocal = Merge mergedToEnv localRootJson
        Merge mergedToLocal localEnvJson
