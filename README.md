# cs2-panorama-ruleset

Rules, patterns and a reference implementation for **server-driven Panorama HUDs in Counter-Strike 2** (`custom_hud_layout`, CounterStrikeSharp).

The ruleset is written to be handed to coding agents and developers before they touch Panorama layouts or the C# code that drives them. It states what the engine allows, which UI building blocks work, how to name and style them, and how to move data to the client without hurting the server tick.

## Contents

| Path | What it is |
| --- | --- |
| [`RULESET.md`](RULESET.md) | The ruleset: engine limits, allowed CSS, naming, visual tokens, UI patterns, architecture, performance, snippets, definition of done |
| [`examples/src/`](examples/src/) | C# reference implementation (CounterStrikeSharp plugin): generic `HudLayout` with diff caches and transmit filter, a paginated menu, keyboard navigation |
| [`examples/panorama/`](examples/panorama/) | Matching layout (`.xml`) and stylesheet (`.vcss`) |

## Using it with an agent

Give the agent `RULESET.md` as context for any Panorama task, for example:

> Follow RULESET.md for every change to Panorama layouts, stylesheets or HUD C# code. Check your result against its definition of done.

Section §0 defines the precedence when the ruleset conflicts with an existing layout.

## Building the examples

```bash
dotnet build examples/Panorama.Examples.csproj -c Release
```

Requires the .NET 10 SDK. The project references `CounterStrikeSharp.API` 1.0.376 and treats warnings as errors. CI builds it on every push.

The examples compile against the API but are **not tested in game**. The Panorama files are not compiled here; publish them in a Workshop addon under `panorama/layout/custom_game/` and `panorama/styles/custom_game/`.

## License

[MIT](LICENSE)
