# mcp-server-for-revit

The MCP server component of [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit).
It exposes Revit operations as MCP tools that AI clients can call.

> [!NOTE]
> This server needs the mcp-servers-for-revit Revit plugin installed and running inside Revit. See the
> [project README](../README.md) for setup instructions.

## How it fits together

```
AI client  <--- MCP over stdio --->  this server  <--- JSON-RPC over TCP :8080 --->  Revit plugin
```

Most tools are thin passthroughs: they publish a JSON schema, forward the arguments to the plugin
under the tool's own command name, and render the reply. Three tools (`store_project_data`,
`store_room_data`, `query_stored_data`) are answered locally from a SQLite database and never reach
Revit.

## Layout

| Path | Contents |
| --- | --- |
| `Program.cs` | Host setup: stdio transport, stderr logging, DI |
| `Revit/` | The socket client, the request gate, and the wire contract with the plugin |
| `Tools/` | One class per MCP tool |
| `Models/` | Parameter types shared by several tools |
| `Data/` | The local SQLite store |

## Building

```bash
dotnet build
dotnet test ../tests/server/RevitMcpServer.Tests.csproj
```

See the [Development section](../README.md#development) of the project README for publishing and for
regenerating the tool schema snapshot.

## Configuration

| Environment variable | Default | Purpose |
| --- | --- | --- |
| `REVIT_MCP_HOST` | `localhost` | Host the Revit plugin listens on |
| `REVIT_MCP_PORT` | `8080` | Port the Revit plugin listens on |
| `REVIT_MCP_CONNECTTIMEOUTSECONDS` | `5` | TCP connect timeout |
| `REVIT_MCP_REQUESTTIMEOUTSECONDS` | `120` | How long to wait for a command result |
| `REVIT_MCP_DATABASEPATH` | `%LocalAppData%\mcp-servers-for-revit\revit-data.db` | Local SQLite database file |
