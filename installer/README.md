# Installer

Builds `mcp-servers-for-revit-Setup-vX.Y.Z.exe`: a per-user wizard that installs the Revit plugin
for whichever Revit versions are on the machine, then wires up the AI clients.

## Why it exists

Installing by hand takes eight steps, and the two that actually defeat people are not the file
copying — they are enabling the commands in Revit's Settings dialog (nothing works until you do)
and hand-editing `claude_desktop_config.json`, a hidden file needing a Windows path with every
backslash doubled.

## How the work is split

| Part | Does |
| --- | --- |
| `mcp-servers-for-revit.iss` (Inno Setup) | Detects Revit versions, asks which to set up, copies files, registers the uninstaller |
| `mcp-server-for-revit.exe setup` | Enables all commands, merges the AI client configuration |

The JSON editing lives in the server executable rather than the installer's Pascal script because
it is real merging work — other MCP servers already configured must survive — and it can be unit
tested (`tests/server/SetupTests.cs`). It also means users can re-run `setup` later without
reinstalling, for example after installing a new Revit version.

## Layout it installs

The server executable is shared by every Revit version and installed once, because copying an
~80 MB file into eight add-in folders would be absurd:

```
%LocalAppData%\Programs\mcp-servers-for-revit\mcp-server-for-revit.exe
%AppData%\Autodesk\Revit\Addins\<year>\mcp-servers-for-revit.addin
%AppData%\Autodesk\Revit\Addins\<year>\revit_mcp_plugin\...
```

Everything is per-user, so no administrator prompt appears.

## Building locally

Needs [Inno Setup 6](https://jrsoftware.org/isdl.php) and a Windows machine.

```powershell
# 1. Publish the server and build the plugin for at least one Revit version
dotnet publish server -c Release -r win-x64 `
  -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o server-publish
dotnet build mcp-servers-for-revit.sln -c "Release R25"

# 2. Assemble the payload the installer reads from
./scripts/build-installer-payload.ps1 -ServerPublishDir server-publish

# 3. Compile
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0 installer\mcp-servers-for-revit.iss
```

The result lands in `installer-output/`.

## Known limitations

- Re-running the installer and un-ticking a Revit version does not remove files already installed
  for it; a full uninstall does.
- A non-zero exit from the `setup` step is not surfaced as an installer error. The Start Menu's
  **Check my mcp-servers-for-revit setup** shortcut runs `doctor`, which reports the problem.

## Code signing

An unsigned installer triggers SmartScreen's "Windows protected your PC" warning. A technical user
clicks through it; the audience this wizard exists for does not. Signing is not wired up here —
it needs a certificate, either purchased or granted free to open source projects by
[SignPath](https://signpath.io/). Add `SignTool` to `[Setup]` once one is available.
