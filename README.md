# ScoobSoundPacker

ScoobSoundPacker is a simple tool for combining multiple FiveM engine sound resources into one clean resource folder.

It copies the `audioconfig` and `sfx` folders from each sound pack, creates the needed output layout, and writes a combined `fxmanifest.lua`.

## Requirements

- Windows
- .NET 8 SDK
- FiveM engine sound resources with `audioconfig`, `sfx`, and/or `fxmanifest.lua`

## Input Folder Setup

Put each engine sound resource inside one main folder.

Example:

```text
MySoundPacks/
  supra2jz/
    audioconfig/
    sfx/
    fxmanifest.lua
  mustangv8/
    audioconfig/
    sfx/
    fxmanifest.lua
  rb26/
    audioconfig/
    sfx/
    fxmanifest.lua
```

If your input folder does not contain subfolders, ScoobSoundPacker will treat the input folder itself as one resource.

## How To Use

Run the tool without arguments for guided prompts:

```powershell
dotnet run
```

Or use the command line:

```powershell
dotnet run -- pack --input "C:\Path\To\MySoundPacks" --output "C:\Path\To\CombinedEngineSounds"
```

Short version:

```powershell
dotnet run -- pack -i "C:\Path\To\MySoundPacks" -o "C:\Path\To\CombinedEngineSounds"
```

## Options

Use `--overwrite` if duplicate files should be replaced by later resources:

```powershell
dotnet run -- pack -i "C:\Path\To\MySoundPacks" -o "C:\Path\To\CombinedEngineSounds" --overwrite
```

Use `--dat10` for audio hashes that need `dat10`/synth manifest lines:

```powershell
dotnet run -- pack -i "C:\Path\To\MySoundPacks" -o "C:\Path\To\CombinedEngineSounds" --dat10 supra2jz,mustangv8
```

## Add To FiveM

After packing, add the output folder to your server resources folder.

Then add this to `server.cfg`:

```cfg
ensure CombinedEngineSounds
```

Use the actual name of your output resource folder.

## License

This project is free to use, modify, and share for non-commercial purposes only.

You may not sell this tool, include it in paid tools, use it in commercial projects, or rebrand it as your own. See the `LICENSE` file for full terms.
