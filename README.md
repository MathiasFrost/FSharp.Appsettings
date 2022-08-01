# FSharp.Appsettings - _FSharp_

Environment-sensitive `appsettings.json` handler.

## Committing

Important to run this before committing _(assuming you have GPG key set up)_

```shell
git config commit.gpgsign true
```

## Usage

Define `FSHARP_ENVIRONMENT` as an environment variable. For development this will normally be done by creating a
launchSettings.json file:

```json5
{
	"profiles": {
		"Development": {
			"commandName": "Project",
			"environmentVariables": {
				"FSHARP_ENVIRONMENT": "Development"
			}
		}
	}
}
```

You can then run this code to load one or two `appsettings.json` files with this code:

```f#
open FSharp.Appsettings

let appsettings = Appsettings.Load ()

// Or by using a type
let typedAppsettings = Appsettings.LoadTyped<Model> ()
```

`appsettings.json` will always be loaded, while `appsettings.{FSHARP_ENVIRONMENT}.json` will be loaded if
FSHARP_ENVIRONMENT is defined as an environment variable.  
Properties from `appsettings.json` will be overwritten by the environment specific
appsettings.  
Arrays will be added together if a value does not already exist in the array.

## Example

```json5
// appsettings.json
{
	"Env": "Root",
	"CORS": {
		"AllowedOrigins": [
			"https://localhost:3000", "https://fsharp.org"
		]
	},
	"Logging": {
		"LogLevel": {
			"Default": "Debug",
			"System": "Information",
			"Microsoft": "Information",
			"Test": "Critical"
		}
	},
	"OnlyRoot": "Root"
}
```

```json5
// appsettings.Development.json (FSHARP_ENVIRONMENT=Development)
{
	"Env": "Development",
	"CORS": {
		"AllowedOrigins": [
			"https://localhost:3000"
		]
	},
	"Logging": {
		"LogLevel": {
			"Default": "Debug",
			"System": "Information",
			"Microsoft": "Information"
		}
	},
	"OnlyDev": "Dev"
}
```

```json5
// Resulting object after Appsettings.load ()
{
	"Env": "Development",
	"CORS": {
		"AllowedOrigins": [
			"https://localhost:3000", "https://fsharp.org"
		]
	},
	"Logging": {
		"LogLevel": {
			"Default": "Debug",
			"System": "Information",
			"Microsoft": "Information",
			"Test": "Critical"
		}
	},
	"OnlyDev": "Dev",
	"OnlyRoot": "Root"
}
```