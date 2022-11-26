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

    /// Find out which field type the JsonNode is
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

    /// Check if JsonArray array contains a JsonNode
    let private Exists (arr: JsonArray) (node: JsonNode) : bool = arr |> Seq.exists (fun x -> x.ToJsonString() = node.ToJsonString())

    /// Stringify a JsonNode and parse it into a new JsonNode
    let private CopyNode (node: JsonNode) = node.ToJsonString() |> JsonNode.Parse

    /// Add array elements from a that does not exist in b
    let rec private MergeArray (a: JsonArray) (b: JsonArray) : unit =
        let elementA = a.GetEnumerator()

        while elementA.MoveNext() do
            let copyA = CopyNode elementA.Current
            if not (Exists b copyA) then b.Add(copyA)

    /// Add the values from a that does not exist in b
    and private Merge (a: JsonObject) (b: JsonObject) : JsonObject =
        let fieldsA = a.GetEnumerator()

        while fieldsA.MoveNext() do
            match fieldsA.Current.Value with
            | Value -> // A is value
                match TryFind fieldsA.Current.Key b with
                | Some _ -> () // If field exists in b, don't add regardless of type
                | None -> b.Add(fieldsA.Current.Key, CopyNode fieldsA.Current.Value) // If field does not exist in b, we also don't care about the type of a's field
            | Object -> // A is object
                match TryFind fieldsA.Current.Key b with
                | Some fieldB ->
                    match fieldB with
                    | Value -> () // If field B is a value we simply replace object A with the value
                    | Object -> Merge (fieldsA.Current.Value.AsObject()) (fieldB.AsObject()) |> ignore // Ignore because object itself is mutated
                    | Array -> () // If field B is an array we simply replace object A with the array
                | None -> b.Add(fieldsA.Current.Key, CopyNode fieldsA.Current.Value)
            | Array -> // A is array
                match TryFind fieldsA.Current.Key b with
                | Some fieldB ->
                    match fieldB with
                    | Value -> () // If field B is a value we simply replace array A with the value
                    | Object -> () // If field B is an object we simply replace array A with the object
                    | Array -> MergeArray (fieldsA.Current.Value.AsArray()) (fieldB.AsArray()) // If both is array we do exclusive array merge
                | None -> b.Add(fieldsA.Current.Key, CopyNode fieldsA.Current.Value)

        b

    /// Load appsettings files, merge them and return a JsonObject
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
