# FSharp.Appsettings - _F#_

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

// Deserialize to type
open System.Text.Json

let required = appsettings.Deserialize<RequiredConfig>()
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

## Building

The `appsettings.json` files must be copied over to output directory. This is achieved by adding the following to
your `.fsproj`.

```xml

<ItemGroup>
    <Content Include="appsettings*.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
    </Content>
</ItemGroup>
```

## Merge rules

Describes what happens when a field from appsettings A encounters a field with the same name from appsettings B

1. Value A - Value B: Value B overwrites value A
2. Object A - Object B: Recursion
3. Array A - Array B: Items from array A are added to array B if not already present
4. Value A - Object B: Object B replaces value A
5. Value A - Array B: Array B replaces value A
6. Object A - Value B: Value B replaces Object A
7. Object A - Array B: Array B replaces Object A
8. Array A - Value B: Value B replaces Array A
9. Array A - Object B: Object B replaces Array A

## Committing

Important to run this before committing _(assuming you have GPG key set up)_

```shell
git config commit.gpgsign true
```