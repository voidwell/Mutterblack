# Mutterblack

[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/voidwell/mutterblack/build-test.yml?branch=main&style=for-the-badge)](https://github.com/voidwell/mutterblack/actions/workflows/build-test.yml)
[![Latest Release](https://img.shields.io/github/v/release/voidwell/mutterblack?style=for-the-badge)](https://github.com/voidwell/mutterblack/releases/latest)
[![MIT License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

Mutterblack is a discord bot primarily repsonsible for providing planetside 2 stats. It relies on the [Voidwell](https://voidwell.com) API and requires credentials to access its endpoints. As it stands, it is not possible to run a version of this bot locally as these endpoints are restricted.

### Adding Mutterblack to your Discord server
If you would like to use Mutterblack, an admin of your server needs to go to this link:    
https://discord.com/oauth2/authorize?client_id=439194558270537728&scope=bot%20applications.commands
 
## Running your own instance

### Requirements
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), or Docker to build and run the included `Dockerfile`.
* A Discord application with a bot token, from the [Discord Developer Portal](https://discord.com/developers/applications). No privileged gateway intents are needed.
* Voidwell API client credentials (client id and secret) for the OIDC provider at `Voidwell:TokenServiceAddress`. These are restricted, so without them the Planetside commands will not work.

### Configuration
Settings are read from `appsettings.json`, then `appsettings.{Environment}.json` (optional), then environment variables. Environment variables override the files, and `__` separates nested keys (e.g. `Discord__Token`). Required settings are validated at startup and the app will not start if one is missing.

| Setting | Required | Description |
| --- | --- | --- |
| `Discord:Token` | Yes | The bot token. |
| `Discord:GuildId` | No | A guild id, for guild-scoped use. |
| `Discord:GatewayIntents` | No | Gateway intents to request. Defaults to `Guilds`. |
| `Discord:LogSeverity` | No | Discord.Net log level. Defaults to `Info`. |
| `Voidwell:TokenServiceAddress` | Yes | The OIDC token endpoint the bot requests access tokens from. |
| `Voidwell:ClientId` | Yes | Voidwell API client id. |
| `Voidwell:ClientSecret` | Yes | Voidwell API client secret. |
| `Voidwell:ClientScopes` | Yes | Scopes to request. Defaults to `voidwell-daybreakgames`. For multiple scopes use `Voidwell__ClientScopes__0`, `Voidwell__ClientScopes__1`, etc. |

The bot fetches its Voidwell access token itself, reuses it until it expires, and requests a new one if the API rejects it.

### Running
Keep secrets out of source control. For local development use environment variables or a git-ignored `appsettings.Development.json`:
```json
{
  "Discord": { "Token": "your-bot-token" },
  "Voidwell": {
    "TokenServiceAddress": "https://auth.example.com/connect/token",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```
```bash
DOTNET_ENVIRONMENT=Development dotnet run --project src/Mutterblack.App
```

With Docker:
```bash
docker build -t mutterblack .
docker run --rm   -e Discord__Token=your-bot-token   -e Voidwell__TokenServiceAddress=https://auth.example.com/connect/token   -e Voidwell__ClientId=your-client-id   -e Voidwell__ClientSecret=your-client-secret   mutterblack
```

### Development
```bash
dotnet build
dotnet format Mutterblack.slnx --verify-no-changes --severity error
```
The Debug configuration treats warnings and code-style issues as errors.

## Slash Commands
The following commands are available:
* `/invite` - Get an invite link to add this bot to your server!
* `/ps2 player <character name>` - Get stats for a player.
* `/ps2 player <character name> <weapon name>` - Get weapon stats for a player.
* `/ps2 outfit <outfit tag>` - Get outfit stats
* `/ps2 weapon <weapon name>` - Get weapon stats

The Player and Outfit commands also support an optional Platform Type argument with possible values PC, PS4-US, and PS4-EU

Notes:
* In some cases if you're getting a bad match and you know the ID of the character or weapon you're trying to look up you may use that instead of a name.    
* Queries are not case sensitive.
* Character names and outfit tags must be the full name.
* Partial weapon names are allowed and it will try to find the best match (i.e "msw" will return results for the "MSW-R").
