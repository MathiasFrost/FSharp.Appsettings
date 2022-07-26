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

let appsettings = Appsettings.Load
```

`appsettings.json` will always be loaded, while `appsettings.{FSHARP_ENVIRONMENT}.json` will be loaded if
FSHARP_ENVIRONMENT is defined as an environment variable.  
Properties from `appsettings.json` will be overwritten by the environment specific
appsettings.  
Arrays will be TODO:...