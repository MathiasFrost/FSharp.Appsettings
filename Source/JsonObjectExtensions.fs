namespace FSharp.Appsettings

open System.Runtime.CompilerServices
open System.Text.Json.Nodes

[<Extension>]
type JsonObjectExtensions =

    [<Extension>]
    static member inline GetPropertyValue(xs: JsonObject, propertyName: string) =
        match xs.TryGetPropertyValue propertyName with
        | true, x -> x
        | false, _ -> failwith $"Could not find property {propertyName}"
