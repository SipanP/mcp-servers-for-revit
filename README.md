[![Cover Image](./assets/cover.png?v=2)](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit)

# mcp-servers-for-revit

**Connect AI assistants to Autodesk Revit via the Model Context Protocol.**

mcp-servers-for-revit enables AI clients like Claude, Cline, and other MCP-compatible tools to read, create, modify, and delete elements in Revit projects. It consists of three components: a C# MCP server that exposes tools to AI, a C# Revit add-in that bridges commands into Revit, and a command set that implements the actual Revit API operations.

> [!NOTE]
> This is a fork of the original [revit-mcp](https://github.com/mcp-servers-for-revit/revit-mcp) project with additional tools and functionality improvements.

## Architecture

```mermaid
flowchart LR
    Client["MCP Client<br/>(Claude, Cline, etc.)"]
    Server["MCP Server<br/><code>server/</code>"]
    Plugin["Revit Plugin<br/><code>plugin/</code>"]
    CommandSet["Command Set<br/><code>commandset/</code>"]
    Revit["Revit API"]

    Client <-->|stdio| Server
    Server <-->|JSON-RPC over TCP| Plugin
    Plugin -->|loads| CommandSet
    CommandSet -->|executes| Revit
```

The **MCP Server** (C#) translates tool calls from AI clients into JSON-RPC messages over a TCP socket. The **Revit Plugin** (C#) runs inside Revit, listens for those messages, and dispatches them to the **Command Set** (C#), which executes the actual Revit API operations and returns results back up the chain.

## Requirements

- **Autodesk Revit 2020 - 2027** (any supported version)
- **Windows** - the MCP server ships as a self-contained executable, so no .NET runtime install is needed

## Quick Start (Installer)

1. Download `mcp-servers-for-revit-Setup-vX.Y.Z.exe` from the [Releases](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit/releases) page

2. Run it. It finds your Revit versions, installs the plugin, enables every command, and points
   Claude Desktop and Claude Code at the server. No administrator rights are needed.

3. Start Revit — if prompted about an unknown add-in, click **Always Load**

4. Restart Claude Desktop

That is the whole setup. To check it afterwards, run **Check my mcp-servers-for-revit setup** from the
Start Menu, or:

```bash
"%LocalAppData%\Programs\mcp-servers-for-revit\mcp-server-for-revit.exe" doctor
```

It reports what is wired up and what is not, and names the fix for anything wrong.

> [!NOTE]
> The installer is not code-signed yet, so Windows SmartScreen may warn on first run. Choose
> **More info → Run anyway**. See [installer/README.md](installer/README.md#code-signing).

## Manual Install (Using a Release ZIP)

Prefer this if you want to control exactly what is installed, or you are installing for one Revit
version only.

1. Download the ZIP for your Revit version from the [Releases](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit/releases) page (e.g., `mcp-servers-for-revit-v1.0.0-Revit2025.zip`)

2. Extract the ZIP and copy the contents to your Revit addins folder:
   ```
   %AppData%\Autodesk\Revit\Addins\<your Revit version>\
   ```
   After copying you should have:
   ```
   Addins/2025/
   ├── mcp-servers-for-revit.addin
   └── revit_mcp_plugin/
       ├── RevitMCPPlugin.dll
       ├── ...
       └── Commands/
           └── RevitMCPCommandSet/
               ├── command.json
               └── 2025/
                   ├── RevitMCPCommandSet.dll
                   └── ...
   ```

3. Configure the MCP server in your AI client (see [MCP Server Setup](#mcp-server-setup))

4. Start Revit — if prompted about an unknown add-in, click **Always Load**

5. In Revit, click the **Settings** button on the mcp-servers-for-revit ribbon tab, enable the commands you want to use, and click **Save**

   Or skip this step by letting the server enable everything for you:
   ```bash
   "%AppData%\Autodesk\Revit\Addins\2025\revit_mcp_plugin\mcp-server-for-revit.exe" setup
   ```
   The same command also writes your AI client configuration, so step 3 becomes unnecessary too.

## MCP Server Setup

`mcp-server-for-revit.exe` is included in the release ZIP and installed alongside the plugin, so
there is nothing extra to download. After extracting the release it lives at:

```
%AppData%\Autodesk\Revit\Addins\<your Revit version>\revit_mcp_plugin\mcp-server-for-revit.exe
```

**Claude Code**

Run this in a **terminal** (not inside Claude Code), substituting your Revit version:

```bash
claude mcp add mcp-server-for-revit -- "%AppData%\Autodesk\Revit\Addins\2025\revit_mcp_plugin\mcp-server-for-revit.exe"
```

**Claude Desktop**

Claude Desktop → Settings → Developer → Edit Config → `claude_desktop_config.json`:

```json
{
    "mcpServers": {
        "mcp-server-for-revit": {
            "command": "C:\\Users\\<you>\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2025\\revit_mcp_plugin\\mcp-server-for-revit.exe"
        }
    }
}
```

The server talks to the plugin on `localhost:8080`. Override with the `REVIT_MCP_HOST` and
`REVIT_MCP_PORT` environment variables if you need to.

Restart Claude Desktop. When you see the hammer icon, the MCP server is connected.

![Claude Desktop connection](./assets/claude.png)

## Revit Plugin Setup

If using a release ZIP, the plugin is already included. For manual installation:

1. Build the plugin from `plugin/` (see [Development](#development))
2. Copy `mcp-servers-for-revit.addin` to `%AppData%\Autodesk\Revit\Addins\<version>\`
3. Copy the `revit_mcp_plugin/` folder to the same addins directory

## Command Set Setup

If using a release ZIP, the command set is pre-installed inside the plugin. For manual installation:

1. Build the command set from `commandset/` (see [Development](#development))
2. Inside the plugin's installation directory, create `Commands/RevitMCPCommandSet/<year>/`
3. Copy the built DLLs into that folder
4. Copy `command.json` (from repo root) into `Commands/RevitMCPCommandSet/`

## Supported Tools

| Tool | Description |
| ---- | ----------- |
| `get_current_view_info` | Get current active view info |
| `get_current_view_elements` | Get elements from the current active view |
| `get_available_family_types` | Get available family types in current project |
| `get_selected_elements` | Get currently selected elements |
| `get_material_quantities` | Calculate material quantities and takeoffs |
| `ai_element_filter` | Intelligent element querying tool for AI assistants |
| `analyze_model_statistics` | Analyze model complexity with element counts |
| `create_point_based_element` | Create point-based elements (door, window, furniture) |
| `create_line_based_element` | Create line-based elements (wall, beam, pipe) |
| `create_surface_based_element` | Create surface-based elements (floor, ceiling, roof) |
| `create_grid` | Create a grid system with smart spacing generation |
| `create_level` | Create levels at specified elevations |
| `create_room` | Create and place rooms at specified locations |
| `create_dimensions` | Create dimension annotations in the current view |
| `create_structural_framing_system` | Create a structural beam framing system |
| `delete_element` | Delete elements by ID |
| `operate_element` | Operate on elements (select, setColor, hide, etc.) |
| `color_elements` | Color elements based on a parameter value |
| `tag_all_walls` | Tag all walls in the current view |
| `tag_all_rooms` | Tag all rooms in the current view |
| `export_room_data` | Export all room data from the project |
| `store_project_data` | Store project metadata in local database |
| `store_room_data` | Store room metadata in local database |
| `query_stored_data` | Query stored project and room data |
| `send_code_to_revit` | Send C# code to Revit to execute |
| `say_hello` | Display a greeting dialog in Revit (connection test) |

## Testing

The test project uses [Nice3point.TUnit.Revit](https://github.com/Nice3point/RevitUnit) to run integration tests against a live Revit instance. No separate addin installation is required — the framework injects into the running Revit process automatically.

### Prerequisites

- **.NET 10 SDK** — required by Nice3point.Revit.Sdk 6.2.3. Install via `winget install Microsoft.DotNet.SDK.10`
- **Autodesk Revit 2027** (or 2026, or 2025) — must be installed and licensed on your machine

### Running Tests

1. Open Revit 2027 (or 2026, or 2025) and wait for it to fully load
2. Run the tests from the command line:

```bash
# For Revit 2027
dotnet test -c Debug.R27 -r win-x64 tests/commandset

# For Revit 2026
dotnet test -c Debug.R26 -r win-x64 tests/commandset

# For Revit 2025
dotnet test -c Debug.R25 -r win-x64 tests/commandset
```

> **Note:** The `-r win-x64` flag is required on ARM64 machines because the Revit API assemblies are x64-only.

Alternatively, you can use `dotnet run`:

```bash
cd tests/commandset
dotnet run -c Debug.R26
```

### IDE Support

- **JetBrains Rider** — enable "Testing Platform support" in Settings > Build, Execution, Deployment > Unit Testing > Testing Platform
- **Visual Studio** — tests should be discoverable through the standard Test Explorer

### Test Structure

| Directory | Purpose |
|-----------|---------|
| `tests/commandset/AssemblyInfo.cs` | Global `[assembly: TestExecutor<RevitThreadExecutor>]` registration |
| `tests/commandset/Architecture/` | Tests for level and room creation commands |
| `tests/commandset/DataExtraction/` | Tests for model statistics, room data export, and material quantities |
| `tests/commandset/ColorSplashTests.cs` | Tests for color override functionality |
| `tests/commandset/TagRoomsTests.cs` | Tests for room tagging functionality |

### Writing New Tests

Test classes inherit from `RevitApiTest` and use TUnit's async assertion API:

```csharp
public class MyTests : RevitApiTest
{
    private static Document _doc;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task MyTest_Condition_ExpectedResult()
    {
        var elements = new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .ToElements();

        await Assert.That(elements.Count).IsGreaterThan(0);
    }
}
```

## Development

### MCP Server

The server is a .NET 10 console application in `server/`, built on the official
[C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk). It speaks MCP over stdio to the AI
client and JSON-RPC over a TCP socket to the Revit plugin.

```bash
dotnet build server
dotnet test tests/server/RevitMcpServer.Tests.csproj
```

To produce the executable that ships in a release:

```bash
dotnet publish server -c Release -r win-x64 \
  -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Publishing is deliberately **not** trimmed: the MCP SDK builds tool schemas by reflection and a
trimmed build fails at runtime.

Tool names and their JSON schemas are the server's public contract, so they are snapshotted in
`tests/server/Fixtures/tool-schemas.json`. After an intentional change, regenerate the snapshot and
review the diff:

```bash
REVIT_MCP_UPDATE_SNAPSHOTS=1 dotnet test tests/server/RevitMcpServer.Tests.csproj
```

### Revit Plugin + Command Set

Open `mcp-servers-for-revit.sln` in Visual Studio. The solution contains the plugin, command set and MCP server projects.

> [!NOTE]
> The MCP server projects appear in the solution but are deliberately **not** built by the
> `R20`-`R27` configurations: the server has no Revit dependency, and Revit 2020-2024 are built with
> Visual Studio's msbuild, which cannot target `net10.0`. Build the server on its own
> (`dotnet build server`) or right-click the project in Visual Studio. Build configurations target Revit 2020-2027:

- **Revit 2020-2024**: .NET Framework 4.8 (`Release R20` through `Release R24`)
- **Revit 2025-2026**: .NET 8 (`Release R25`, `Release R26`)
- **Revit 2027**: .NET 10 (`Release R27`)

Building the solution automatically assembles the complete deployable layout in `plugin/bin/AddIn <year> <config>/` - the command set is copied into the plugin's `Commands/` folder as part of the build.

## Project Structure

```
mcp-servers-for-revit/
├── mcp-servers-for-revit.sln    # Combined solution (server + plugin + commandset + tests)
├── command.json     # Command set manifest
├── installer/       # Inno Setup wizard - one-click install for end users
├── server/          # MCP server (C#) - tools exposed to AI clients
├── plugin/          # Revit add-in (C#) - socket bridge inside Revit
├── commandset/      # Command implementations (C#) - Revit API operations
├── tests/server/     # MCP server tests (C#) - TUnit, no Revit needed
├── tests/commandset/ # Integration tests (C#) - TUnit tests against live Revit
├── assets/          # Images for documentation
├── .github/         # CI/CD workflows, contributing guide, code of conduct
├── LICENSE
└── README.md
```

## Releasing

A single `v*` tag drives the entire release. The [release workflow](.github/workflows/release.yml) automatically:

- Runs the MCP server test suite and publishes it as a self-contained `win-x64` executable
- Builds the Revit plugin + command set for Revit 2020-2027
- Compiles the [installer](installer/README.md) covering every Revit version in one download
- Bundles the server executable into each plugin payload so the two always ship in lockstep
- Creates a GitHub release with the installer plus `mcp-servers-for-revit-vX.Y.Z-Revit<year>.zip` assets

To create a release:

1. Run the bump script (updates `server/RevitMcpServer.csproj` and `plugin/Properties/AssemblyInfo.cs`, then commits and tags):
   ```powershell
   ./scripts/release.ps1 -Version X.Y.Z
   ```

2. Push to trigger the workflow:
   ```bash
   git push origin main --tags
   ```

## Acknowledgements

This project is a fork of the work by the [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit) team. The original repositories:

- [revit-mcp](https://github.com/mcp-servers-for-revit/revit-mcp) - MCP server
- [revit-mcp-plugin](https://github.com/mcp-servers-for-revit/revit-mcp-plugin) - Revit plugin
- [revit-mcp-commandset](https://github.com/mcp-servers-for-revit/revit-mcp-commandset) - Command set

Thank you to the original authors for creating the foundation that this project builds upon.

## License

[MIT](LICENSE)
