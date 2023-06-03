module FSharp.Appsettings

open System
open System.IO
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Text.Json.Nodes

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
    static member inline GetValue<'T>(xs: JsonObject, propertyName: string) = xs.GetNode(propertyName).GetValue<'T>()

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
let inline value<'T> (propertyName: string) (json: JsonNode) =
    try
        json.AsObject().GetValue<'T>(propertyName)
    with
    | :? InvalidOperationException -> invalidOp $"Could not find %s{typeof<'T>.Name} property %s{json.GetPath()}.%s{propertyName}"

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
let inline dict (propertyName: string) (json: JsonNode) =
    try
        [ for pair in json.AsObject().GetObject(propertyName) do
              yield (pair.Key, pair.Value) ]
    with
    | :? InvalidOperationException -> invalidOp $"Could not find array property %s{json.GetPath()}.%s{propertyName}"

/// TODOC
let inline iter (action: JsonNode -> unit) (jsonArray: JsonArray) =
    for el in jsonArray do
        action el

/// TODOC
let inline iteri (action: int -> JsonNode -> unit) (jsonArray: JsonArray) =
    let mutable i = 0

    for el in jsonArray do
        action i el
        i <- i + 1

/// TODOC
let inline iterd (action: string * JsonNode -> unit) (jsonObject: JsonObject) =
    for pair in jsonObject do
        action (pair.Key, pair.Value)

/// TODOC
let inline iterdi (action: int -> string * JsonNode -> unit) (jsonObject: JsonObject) =
    let mutable i = 0

    for pair in jsonObject do
        action i (pair.Key, pair.Value)
        i <- i + 1

/// TODOC
let inline list (jsonArray: JsonArray) =
    [ for el in jsonArray do
          yield el ]

/// TODOC
let inline listd (jsonObject: JsonObject) =
    [ for pair in jsonObject do
          yield (pair.Key, pair.Value) ]

/// Returns an empty "{}" JsonObject
let private EmptyJsonObject () = JsonObject.Parse("{}").AsObject()

/// Read file and return a JsonObject. If it doesn't exist return en empty JsonObject
let private ParseJsonFile filename =
    match File.Exists filename with
    | true ->
        try
            let json = File.ReadAllText filename |> JsonNode.Parse
            json.AsObject()
        with
        | e -> raise (Exception($"Unable to parse %s{filename}", e))
    | false -> EmptyJsonObject()

/// Find out which prop type the JsonNode is
let private (|Value|Object|Array|Null|) (node: JsonNode) =
    if node = null then
        Null
    else
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

// Wrapper around zip that set JsonNode in b to null if not exist from a
let private ZipOptionalB (a: (string * JsonNode) list) (b: (string * JsonNode) list) : ((string * JsonNode) * (string * JsonNode)) list =
    a
    |> List.map (fun (keyA, valA) ->
        let valB: JsonNode =
            match b |> List.tryFind (fun (keyB, _) -> keyA = keyB) with
            | Some (_, x) -> x
            | None -> null

        ((keyA, valA), (keyA, valB)))

/// TODOC
let private CopyNode (jsonNode: JsonNode) = jsonNode.ToJsonString() |> JsonNode.Parse

/// Layer b on top of a
let rec private Merge (a: JsonNode) (b: JsonNode) : unit =
    match a, b with
    | Object, Object ->
        let objA = a.AsObject()
        let objB = b.AsObject()

        for pairB in objB do
            let valA = objA[pairB.Key]
            let valB = pairB.Value

            if objA.ContainsKey pairB.Key then
                match valA, valB with
                | Value, _ when objA.Remove pairB.Key -> objA.TryAdd(pairB.Key, CopyNode pairB.Value) |> ignore // If a is value, replace a with b
                | Object, Object -> Merge valA valB // If both are objects merge them recursively
                | Array, Array -> Merge valA valB // If both are arrays merge them recursively
                | _, Object when objA.Remove pairB.Key -> objA.TryAdd(pairB.Key, CopyNode pairB.Value) |> ignore // If types are different, just overwrite
                | _, Array when objA.Remove pairB.Key -> objA.TryAdd(pairB.Key, CopyNode pairB.Value) |> ignore // If types are different, just overwrite
                | _, Value when objA.Remove pairB.Key -> objA.TryAdd(pairB.Key, CopyNode pairB.Value) |> ignore // If types are different, just overwrite
                | _ -> ()
            else // If a does not have b, simply add b to a
                objA.Add(pairB.Key, CopyNode pairB.Value)
    | Array, Array ->
        let arrA = a.AsArray()
        let arrB = b.AsArray()
        let mutable i = 0

        for elB in arrB do
            if arrA.Count > i then
                let elA = arrA[i]

                match elA, elB with
                | Value, _ -> arrA[i] <- CopyNode elB // If a is value, replace a with b
                | Object, Object -> Merge elA elB // If both are objects merge them recursively
                | Array, Array -> Merge elA elB // If both are arrays merge them recursively
                | _, Object -> arrA[i] <- CopyNode elB // If types are different, just overwrite
                | _, Array -> arrA[i] <- CopyNode elB // If types are different, just overwrite
                | _, Value -> arrA[i] <- CopyNode elB // If types are different, just overwrite
                | _ -> ()
            else // If a does not have b, simply add b to a
                arrA.Add(CopyNode elB)

            i <- i + 1
    | _ -> ()

/// Load appsettings files, merge them and return a JsonObject
let LoadAppsettings () : JsonObject =
    let fsEnv =
        try
            let env = Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT"
            if String.IsNullOrWhiteSpace env then None else Some(env)
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

    Merge rootJson envJson
    Merge rootJson localRootJson
    Merge rootJson localEnvJson

    rootJson

/// Root JsonObject containing the merged appsettings.json values
let appsettings = LoadAppsettings()
