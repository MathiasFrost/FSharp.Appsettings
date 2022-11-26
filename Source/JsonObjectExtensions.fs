namespace FSharp.Appsettings

open System.Runtime.CompilerServices
open System.Text.Json.Nodes

[<Extension>]
type JsonObjectExtensions =

    /// TryGetPropertyValue without the 'Try'. Throw System.ArgumentNullException if field does not exist on object
    [<Extension>]
    static member inline GetPropertyValue(xs: JsonObject, propertyName: string) =
        match xs.TryGetPropertyValue propertyName with
        | true, x -> x
        | false, _ -> nullArg $"Could not find property {propertyName}"
