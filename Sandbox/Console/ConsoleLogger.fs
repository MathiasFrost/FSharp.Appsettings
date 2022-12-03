namespace FSharp.Appsettings.Sandbox.Console

open Microsoft.Extensions.Logging

type ConsoleLogger =
    interface ILogger with
        member this.BeginScope(state) = failwith "todo"
        member this.IsEnabled(logLevel) = logLevel <> LogLevel.None
        member this.Log(logLevel, eventId, state, ``exception``, formatter) = failwith "todo"
