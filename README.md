# FSharp.Appsettings - _FSharp_

Minimalistic environment-sensitive `appsettings.json` loader.

## Usage

Define `FSHARP_ENVIRONMENT` as an environment variable. For development this will normally be done by creating a
launchSettings.json file:

```json5
{
	"$schema": "https://json.schemastore.org/launchsettings.json",
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

`appsettings.json` will always be loaded, while `appsettings.{FSHARP_ENVIRONMENT}.json` will be loaded
if `FSHARP_ENVIRONMENT` is defined as an environment variable.  
Properties from `appsettings.json` will be overwritten by the environment specific appsettings.  
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

**Note:** This NuGet does not support defining secrets by defining `<UserSecretsId>` in the `.csproj` file.  
Instead it detects `appsettings{.FSHARP_ENVIRONMENT?}.local.json` that you can _(should)_ add to `.gitignore`.

## Order of Priority

1. `appsettings.{FSHARP_ENVIRONMENT}.local.json`
2. `appsettings.local.json`
3. `appsettings.{FSHARP_ENVIRONMENT}.json`
4. `appsettings.json`

## Committing

Important to run this before committing _(assuming you have GPG key set up)_

```shell
git config commit.gpgsign true
```