namespace FSharp.Appsettings

open System
open System.IO
open FSharp.Data

type private Parser(str: string) =
    let str = str
    let mutable i = 0
    let pairs = ResizeArray<string * string>()
    let prefixes = ResizeArray<string>()

    let ensure cond =
        if not cond then
            failwith $"Unexpected character: '{str[i]}' at position {i}"

    /// Comment and whitespace skipper courtesy of https://github.com/fsprojects/FSharp.Data/blob/main/src/Json/JsonValue.fs
    let rec skipCommentsAndWhitespace () =
        let skipComment () =
            // Supported comment syntax:
            // - // ...{newLine}
            // - /* ... */
            if i < str.Length && str[i] = '/' then
                i <- i + 1

                if i < str.Length && str[i] = '/' then
                    i <- i + 1

                    while i < str.Length
                          && (str[i] <> '\r' && str[i] <> '\n') do
                        i <- i + 1
                else if i < str.Length && str[i] = '*' then
                    i <- i + 1

                    while i + 1 < str.Length
                          && str[i] <> '*'
                          && str[i + 1] <> '/' do
                        i <- i + 1

                    ensure (
                        i + 1 < str.Length
                        && str[i] = '*'
                        && str[i + 1] = '/'
                    )

                    i <- i + 2

                true
            else
                false

        let skipWhitespace () =
            let initialI = i

            while i < str.Length && Char.IsWhiteSpace str[i] do
                i <- i + 1

            initialI <> i // return true if some whitespace was skipped

        if skipWhitespace () || skipComment () then
            skipCommentsAndWhitespace ()


    /// Get the first value part of a JSON string
    let rec getValue () =

        let start = i
        let mutable inQuotes = false

        while not inQuotes && (str[i] <> ',' || str[i] <> '}') do
            if str[i] = '\\' then
                i <- i + 2
            else
                i <- i + 1

                if str[i] = '"' then
                    inQuotes <- not inQuotes

        let value = str[start..i]
        i <- i + 1

        skipCommentsAndWhitespace ()

        if str[i] = ',' then
            i <- i + 1
            skipCommentsAndWhitespace ()

        while str[i] = '}' && prefixes.Count > 0 do
            prefixes.RemoveAt(prefixes.Count - 1)
            i <- i + 1
            skipCommentsAndWhitespace ()

            if str[i] = ',' then
                i <- i + 1
                skipCommentsAndWhitespace ()

        value

    /// Get the first key part of a JSON string
    and getKey () : string =
        ensure (str[i] = '"')
        let start = i
        i <- i + 1

        while str[i] <> '"' do
            if str[i] = '\\' then
                i <- i + 2
            else
                i <- i + 1

        let key = str[start..i]
        i <- i + 1

        skipCommentsAndWhitespace ()
        ensure (str[i] = ':')
        i <- i + 1

        skipCommentsAndWhitespace ()

        if str[i] = '{' then
            i <- i + 1
            skipCommentsAndWhitespace ()
            prefixes.Add key
            getKey ()
        else
            key

    /// Parse a JSON string into flat key/value pars
    member _.Parse() =
        skipCommentsAndWhitespace ()
        ensure (str[i] = '{')
        i <- i + 1

        let stop = str.LastIndexOf '}'
        skipCommentsAndWhitespace ()

        while i < stop do
            let key = getKey ()

            let fullKey =
                String.concat "__" (List.ofArray (prefixes.ToArray()) @ [ key ])

            let value = getValue ()
            printfn "\npair: %s: %s\n" fullKey value

            pairs.Add(fullKey, value)

        pairs.ToArray() |> Map.ofArray

module Appsettings =

    /// Read file if exists
    let private TryReadFile filename =
        let exists = File.Exists filename

        match exists with
        | true -> Some(File.ReadAllText filename)
        | false -> None

    /// Merge two JSON strings
    let private Merge (x: string) (y: string) =
        let pairs = Parser(x).Parse()
        printfn "%A" pairs
        JsonValue.Parse "{}"

    /// Load appsettings
    let Load: JsonValue =
        let env =
            Environment.GetEnvironmentVariable "FSHARP_ENVIRONMENT"

        let envJson =
            match String.IsNullOrWhiteSpace env with
            | true -> None
            | false -> TryReadFile $"appsettings.{env}.json"

        match TryReadFile "appsettings.json" with
        | Some envRoot when envJson.IsSome -> Merge envRoot envJson.Value // Both merged
        | Some envRoot when envJson.IsNone -> JsonValue.Parse envRoot // Only appsettings.json
        | None when envJson.IsSome -> JsonValue.Parse envJson.Value // Only appsettings.{env}.json
        | _ -> raise (NullReferenceException "No appsettings.json was found") // Both non-existent
