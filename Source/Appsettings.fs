module FSharp.Appsettings

open System
open System.IO
open System.Runtime.CompilerServices
open System.Text.Json.Nodes

/// Returns an empty "{}" JsonObject
let private EmptyJsonObject () = JsonObject.Parse("{}").AsObject()

/// Read file and return a JsonObject. If it doesn't exist return en empty JsonObject
let private ParseJsonFile filename =
    match File.Exists filename with
    | true ->
        (File.ReadAllText filename |> JsonObject.Parse)
            .AsObject()
    | false -> EmptyJsonObject()

/// Find out which prop type the JsonNode is
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

    elementA.Dispose()

/// Add the values from a that does not exist in b
and private Merge (a: JsonObject) (b: JsonObject) : JsonObject =
    let propsA = a.GetEnumerator()

    while propsA.MoveNext() do
        match propsA.Current.Value with
        | Value -> // A is value
            match TryFind propsA.Current.Key b with
            | Some _ -> () // If prop exists in b, don't add regardless of type
            | None -> b.Add(propsA.Current.Key, CopyNode propsA.Current.Value) // If prop does not exist in b, we also don't care about the type of a's prop
        | Object -> // A is object
            match TryFind propsA.Current.Key b with
            | Some propB ->
                match propB with
                | Value -> () // If prop B is a value we simply replace object A with the value
                | Object -> Merge (propsA.Current.Value.AsObject()) (propB.AsObject()) |> ignore // Ignore because object itself is mutated
                | Array -> () // If prop B is an array we simply replace object A with the array
            | None -> b.Add(propsA.Current.Key, CopyNode propsA.Current.Value)
        | Array -> // A is array
            match TryFind propsA.Current.Key b with
            | Some propB ->
                match propB with
                | Value -> () // If prop B is a value we simply replace array A with the value
                | Object -> () // If prop B is an object we simply replace array A with the object
                | Array -> MergeArray (propsA.Current.Value.AsArray()) (propB.AsArray()) // If both is array we do exclusive array merge
            | None -> b.Add(propsA.Current.Key, CopyNode propsA.Current.Value)

    propsA.Dispose()
    b

/// Load appsettings files, merge them and return a JsonObject
let LoadAppsettings () : JsonObject =
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

/// Root JsonObject containing the merged appsettings.json values
let appsettings = LoadAppsettings()

/// Extensions to retrieve JSON property values
[<Extension>]
type JsonObjectExtensions =

    /// TryGetPropertyValue without the 'Try'. Throw System.ArgumentNullException if property does not exist on object
    [<Extension>]
    static member inline GetNode(xs: JsonObject, propertyName: string) =
        match xs.TryGetPropertyValue propertyName with
        | true, x -> x
        | false, _ -> invalidOp $"Could not find property {propertyName}"

    /// TODOC
    [<Extension>]
    static member inline GetPropertyValue<'T>(xs: JsonObject, propertyName: string) = xs.GetNode(propertyName).GetValue<'T>()

    /// TODOC
    [<Extension>]
    static member inline GetObject(xs: JsonObject, propertyName: string) = xs.GetNode(propertyName).AsObject()

    /// TODOC
    [<Extension>]
    static member inline GetArray(xs: JsonObject, propertyName: string) = xs.GetNode(propertyName).AsArray()

    /// TODOC
    [<Extension>]
    static member inline GetEnum<'TEnum when 'TEnum: struct and 'TEnum: (new: unit -> 'TEnum) and 'TEnum :> ValueType>(xs: JsonNode) =
        Enum.Parse<'TEnum>(xs.GetValue<string>())


/// TODOC
let inline object (propertyName: string) (json: JsonNode) =
    try
        json.AsObject().GetObject(propertyName)
    with
    | :? InvalidOperationException -> invalidOp $"Could not find object property %s{json.GetPath()}.%s{propertyName}"

/// TODOC
let inline array (propertyName: string) (json: JsonNode) =
    try
        json.AsObject().GetArray(propertyName)
    with
    | :? InvalidOperationException -> invalidOp $"Could not find array property %s{json.GetPath()}.%s{propertyName}"

/// TODOC
let inline iter (action: JsonNode -> unit) (jsonArray: JsonArray) =
    let enumerator = jsonArray.GetEnumerator()

    while enumerator.MoveNext() do
        action enumerator.Current

    enumerator.Dispose()

/// TODOC
let inline iteri (action: int -> JsonNode -> unit) (jsonArray: JsonArray) =
    let enumerator = jsonArray.GetEnumerator()
    let mutable i = 0

    while enumerator.MoveNext() do
        action i enumerator.Current
        i <- i + 1

    enumerator.Dispose()

/// TODOC
let inline iterd (action: string * JsonNode -> unit) (jsonObject: JsonObject) =
    let enumerator = jsonObject.GetEnumerator()

    while enumerator.MoveNext() do
        action (enumerator.Current.Key, enumerator.Current.Value)

    enumerator.Dispose()

/// TODOC
let inline iterdi (action: int -> string * JsonNode -> unit) (jsonObject: JsonObject) =
    let enumerator = jsonObject.GetEnumerator()
    let mutable i = 0

    while enumerator.MoveNext() do
        action i (enumerator.Current.Key, enumerator.Current.Value)
        i <- i + 1

    enumerator.Dispose()

/// TODOC
let inline list (jsonArray: JsonArray) =
    let enumerator = jsonArray.GetEnumerator()

    let res =
        [ while enumerator.MoveNext() do
              yield enumerator.Current ]

    enumerator.Dispose()
    res

/// TODOC
let inline dict (propertyName: string) (json: JsonNode) =
    try
        let enumerator =
            json
                .AsObject()
                .GetObject(propertyName)
                .GetEnumerator()

        let res =
            [ while enumerator.MoveNext() do
                  yield (enumerator.Current.Key, enumerator.Current.Value) ]

        enumerator.Dispose()
        res
    with
    | :? InvalidOperationException -> invalidOp $"Could not find array property %s{json.GetPath()}.%s{propertyName}"

/// TODOC
let inline value<'T> (propertyName: string) (json: JsonNode) =
    try
        json.AsObject().GetPropertyValue<'T>(propertyName)
    with
    | :? InvalidOperationException -> invalidOp $"Could not find %s{typeof<'T>.Name} property %s{json.GetPath()}.%s{propertyName}"
