# unlimitedSeeds

A Valheim mod that expands the world seed space by extending the internal offset range used by the WorldGenerator.

## What It Does

Vanilla Valheim uses `Random.Range(-10000, 10000)` for five internal offset values (m_offset0-4), limiting all possible worlds to a 40k×40k region of the infinite Perlin noise space. This mod expands that range to ±50000, giving access to terrain configurations that no vanilla seed can produce.

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) for Valheim
2. Copy `unlimitedSeeds.dll` to `Valheim/BepInEx/plugins/`

## Building from Source

Requires .NET SDK and publicized Valheim assemblies.

```bash
dotnet build
```

Output DLL will be in `bin/Debug/`.

## Configuration

Edit `Environment.props` if your Steam library is in a non-standard location.

## License

MIT
