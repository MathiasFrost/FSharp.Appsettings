module FSharp.Appsettings

open System
open System.IO
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Text.Json.Nodes

/// Returns an empty "{}" JsonObject
let private EmptyJsonObject () = JsonObject.Parse("{}").AsObject()

/// Read file and return a JsonObject. If it doesn't exist return en empty JsonObject
let private ParseJsonFile filename =
    match File.Exists filename with
    | true -> (File.ReadAllText filename |> JsonObject.Parse).AsObject()
    | false -> EmptyJsonObject()

let private IsObject (jsonNode: JsonNode) : bool * JsonObject option =
    try
        let obj = jsonNode.AsObject()
        (true, Some obj)
    with _ ->
        (false, None)

/// Layer b on top of a
let rec private Merge (a: JsonObject) (b: JsonObject) : unit =
    if a <> null && b <> null then
        for pairB in b do
            match IsObject pairB.Value, IsObject a[pairB.Key] with
            | (true, Some objB), (true, Some objA) -> objA |> Merge objB
            | _ when not (a.ContainsKey pairB.Key) || a.Remove pairB.Key -> a.TryAdd(pairB.Key, pairB.Value.ToJsonString() |> JsonNode.Parse) |> ignore
            | _ -> ()

/// Load appsettings files, merge them and return a JsonObject
let LoadAppsettings () : JsonObject =
    let fsEnv =
        try
            let env = Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT"
            if env = null then None else Some(env)
        with :? ArgumentNullException ->
            None

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

    Merge rootJson envJson
    Merge rootJson localRootJson
    Merge rootJson localEnvJson

    rootJson

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
        let enumerator = json.AsObject().GetObject(propertyName).GetEnumerator()

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
