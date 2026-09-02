# Scoping: Porting the MCP Server from Node.js to C#

Status: implemented. This document is kept as the record of what was decided and why;
see the pull request that ported `server/` for the delivered result.
Scope: `server/` only. The Revit plugin (`plugin/`) and command set (`commandset/`) are already C# and are **not** changed by this work.

---

## 1. Recommendation in one paragraph

Port `server/` to a .NET 10 console app built on the official
[`ModelContextProtocol` C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), keeping the
existing TCP/JSON-RPC wire contract with the plugin byte-for-byte so **no plugin or command set code
has to change**. Ship the result as a self-contained `win-x64` executable inside the same release ZIP
users already install into their Revit addins folder. That removes the Node.js prerequisite entirely,
puts the server and plugin on one version, and makes the whole repo a single toolchain. Estimated
effort: **5–7 focused days** for one developer, and the port is mostly mechanical — 23 real tools,
each a thin parameter-schema-plus-passthrough wrapper.

---

## 1a. What was actually built

The port landed broadly as scoped. Where reality differed:

- **Target framework is .NET 10**, not .NET 8.
- **The `server/` folder name was kept** (decision 1), and the project was added to
  `mcp-servers-for-revit.sln` with all 16 configuration mappings (decision 3).
- **The C# MCP SDK turned out to be stable at 2.x**, not pre-1.0 as assumed while scoping.
- **Trimming is off, confirmed by experiment**, not by caution: a trimmed build compiles but fails at
  runtime, because the SDK builds tool schemas by reflection. The published executable is ~80 MB
  (~33 MB compressed).
- **Both servers' tool schemas were captured and diffed** rather than eyeballed. The residual
  differences are listed in the pull request and are all either deliberate or limitations of
  `System.Text.Json` schema generation.

---

## 2. What exists today

`server/` is ~2,550 lines of TypeScript on `@modelcontextprotocol/sdk` ^1.7.0 over **stdio**.

### 2.1 Tool inventory (29 files in `server/src/tools/`)

| Category | Count | Files |
| --- | --- | --- |
| Revit passthrough (TCP socket) | 23 | `say_hello`, `get_current_view_info`, `get_current_view_elements`, `get_selected_elements`, `get_available_family_types`, `create_level`, `create_grid`, `create_room`, `create_dimensions`, `create_point_based_element`, `create_line_based_element`, `create_surface_based_element`, `create_structural_framing_system`, `color_elements`, `delete_element`, `operate_element`, `tag_all_rooms`, `tag_all_walls`, `ai_element_filter`, `analyze_model_statistics`, `get_material_quantities`, `export_room_data`, `send_code_to_revit` |
| Local SQLite | 3 | `store_project_data`, `store_room_data`, `query_stored_data` |
| Empty stubs (0 bytes) | 3 | `modify_element`, `search_modules`, `use_module` |

The 23 passthrough tools are all the same shape: a zod schema, a `withRevitConnection` call that
forwards the args under the tool's own name as the JSON-RPC method, and a `JSON.stringify` of the
result into a text content block. Only the schema and the error-message prefix differ between them.

### 2.2 The wire contract with the plugin (`server/src/utils/SocketClient.ts` ↔ `plugin/Core/SocketService.cs`)

This is the piece that constrains the port. Today it is:

1. Server opens a **new TCP connection** to `localhost:8080` per tool call (5 s connect timeout).
2. Server writes **one** JSON-RPC 2.0 request object: `{jsonrpc, method, params, id}`. No framing —
   no length prefix, no delimiter.
3. Plugin does a single `NetworkStream.Read` into an **8192-byte buffer**, deserializes it as a
   `JsonRPCRequest`, dispatches via `ICommandRegistry`, and writes back one JSON-RPC response.
4. Server accumulates bytes and retries `JSON.parse(buffer)` until it succeeds — that is the framing.
5. Server closes the connection. A module-level promise mutex serialises all calls, so only one
   request is ever in flight. 120 s response timeout.

**Keep this contract exactly as-is for v1 of the port.** It is the reason the plugin needs no changes.
See §7 for the two latent bugs in it and when to fix them.

### 2.3 Local database (`server/src/database/`)

`better-sqlite3`, two tables (`projects`, `rooms`) with a cascade FK and four indexes, plus ~276 lines
of query helpers in `service.ts`. The DB file path is `join(__dirname, '..', '..', 'revit-data.db')`,
which resolves to `server/revit-data.db` — i.e. **inside the npx cache directory** when installed the
documented way. That is effectively a data-loss bug (see §7).

### 2.4 Distribution today

Published to npm as `mcp-server-for-revit`; users configure `cmd /c npx -y mcp-server-for-revit`.
`.github/workflows/release.yml` has an `npm-publish` job; `scripts/release.ps1` bumps
`server/package.json` + lockfile alongside `plugin/Properties/AssemblyInfo.cs`.

---

## 3. What changes and what doesn't

**Unchanged:**
- `plugin/` and `commandset/` — zero code changes.
- `command.json`, the addin layout, the Revit-version build matrix (R20–R27).
- The JSON-RPC method names and parameter shapes on the wire.
- The MCP tool names, descriptions, and JSON schemas as the model sees them.

**Changed:**
- `server/` is replaced by a .NET project.
- Install/config instructions in `README.md` (no more `npx`).
- `release.yml` (drop npm job, add `dotnet publish` + bundle the exe).
- `scripts/release.ps1` (bump a csproj `<Version>` instead of `package.json`).
- `.gitignore` (`server/build/`, `server/node_modules/`, `server/revit-data.db`).

---

## 4. Target architecture

### 4.1 Project layout

```
server/                      -> deleted after cutover
src/RevitMcpServer/          -> new
  RevitMcpServer.csproj      net10.0, self-contained publish for win-x64
  Program.cs                 host builder, stdio transport, stderr logging
  Revit/
    RevitClient.cs           TcpClient + JSON-RPC framing (port of SocketClient.ts)
    RevitConnection.cs       SemaphoreSlim gate + connect/timeout (port of ConnectionManager.ts)
    RevitServerOptions.cs    host/port/timeouts, bound from env vars
  Tools/
    SayHelloTool.cs          one class per tool, [McpServerToolType]
    CreateLevelTool.cs
    ... (23 files)
    Data/                    the 3 SQLite-backed tools
  Data/
    RevitDataStore.cs        port of database/service.ts
    Schema.cs                DDL, identical to db.ts
tests/server/
  RevitMcpServer.Tests.csproj
```

`src/` is a new folder; `plugin/`, `commandset/`, `tests/` keep their current top-level positions. If
that feels inconsistent, the alternative is to keep the folder named `server/` and just change its
contents — cheaper for muscle memory and for any external links, and it keeps the diff readable.
**Recommend keeping the folder name `server/`.**

### 4.2 Solution integration — a real gotcha

`mcp-servers-for-revit.sln` defines 16 solution configurations (`Debug R20` … `Release R27`) because
every project is multi-targeted per Revit version. The MCP server runs **outside** Revit and has no
Revit dependency, so it should target plain `net10.0` with plain `Debug|Release`. Adding it to the sln
means writing 16 `ProjectConfigurationPlatforms` mappings that all point at `Debug|Any CPU` or
`Release|Any CPU`.

Two options:
- **(a)** Add it to the sln with the 16 mappings. Everything builds from one solution; the mapping
  block is ugly but written once.
- **(b)** Keep it in its own `server.sln`, built separately in CI.

**Recommend (a)** — one `dotnet build` for contributors is worth the mapping boilerplate, and it means
the server can never silently rot when the plugin changes.

### 4.3 Package choices

| Concern | Today (Node) | Proposed (.NET) |
| --- | --- | --- |
| MCP protocol + stdio | `@modelcontextprotocol/sdk` | `ModelContextProtocol` (official C# SDK, MIT) |
| Hosting / DI / config | none | `Microsoft.Extensions.Hosting` |
| Tool schemas | `zod` | Method signature + `[Description]`, schema generated by the SDK |
| Tool discovery | `fs.readdirSync` + dynamic `import()` | `WithTools<T>()` explicit, or `WithToolsFromAssembly()` |
| SQLite | `better-sqlite3` | `Microsoft.Data.Sqlite` (+ optional Dapper) |
| JSON | built in | `System.Text.Json` |
| Logging | `console.error` | `ILogger` with `LogToStandardErrorThreshold` |
| Tests | none | xunit or TUnit (`global.json` already selects Microsoft.Testing.Platform) |

The C# MCP SDK reached a stable 2.x while this was being scoped, so the pre-1.0 churn risk noted in
early drafts no longer applies. Pin an exact version regardless.

### 4.4 Code shape — before and after

Today, `server/src/tools/say_hello.ts`:

```ts
export function registerSayHelloTool(server: McpServer) {
  server.tool("say_hello", "Display a greeting dialog in Revit. ...",
    { message: z.string().optional().describe("Optional custom message ...") },
    async (args) => {
      try {
        const response = await withRevitConnection(c => c.sendCommand("say_hello", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `Say hello failed: ${...}` }] };
      }
    });
}
```

After:

```csharp
[McpServerToolType]
public sealed class SayHelloTool(RevitConnection revit)
{
    [McpServerTool(Name = "say_hello")]
    [Description("Display a greeting dialog in Revit. Useful for testing the connection between Claude and Revit.")]
    public Task<string> SayHelloAsync(
        [Description("Optional custom message to display in the dialog. Defaults to 'Hello MCP!'")]
        string? message = null,
        CancellationToken ct = default)
        => revit.InvokeAsync("say_hello", new { message }, ct);
}
```

`RevitConnection.InvokeAsync` owns the connect/serialize/timeout/error-to-text behaviour that is
currently copy-pasted into all 23 tool files — so the port is also a de-duplication.

### 4.5 zod → C# schema translation cheatsheet

| zod | C# |
| --- | --- |
| `z.string()` | `string` |
| `z.string().optional()` | `string? x = null` |
| `z.number()` / `.int().positive()` | `double` / `int` (+ range stated in `[Description]`) |
| `z.boolean().default(true)` | `bool x = true` |
| `z.enum(["alphabetic","numeric"])` | `enum NamingStyle { Alphabetic, Numeric }` with `JsonStringEnumConverter`, or `string` with the allowed values in the description |
| `z.array(z.object({...}))` | `IReadOnlyList<LevelSpec>` where `LevelSpec` is a record with `[Description]` per property |
| `.describe(...)` | `[Description(...)]` |

Enum casing is the one place to be careful: the plugin expects the lowercase strings
(`"alphabetic"`, `"auto"`, `"none"`). Verify the serialised value on the wire, not just the schema.

---

## 5. Distribution — the decision that matters most

`npx -y mcp-server-for-revit` is a genuinely good zero-install UX. Anything C# has to earn its place
against it.

| Option | User prerequisite | Config line | Notes |
| --- | --- | --- | --- |
| **A. Self-contained exe in the release ZIP** ⭐ | none | absolute path to the exe | ~15 MB trimmed / ~60 MB untrimmed. Server and plugin ship and version together. Users already unzip into the addins folder, so it costs them nothing extra. |
| B. .NET global tool | .NET SDK | `mcp-server-for-revit` | Smallest download, but an SDK — not just a runtime — is a heavier ask than Node was. `dnx` (.NET 10) gives an npx-like one-shot run, but only for users on the .NET 10 SDK. |
| C. Framework-dependent exe | .NET 10 runtime | absolute path | ~1 MB, but swaps one prerequisite for another. |

**Recommend A**, optionally also publishing B for developers. With A the README config becomes:

```bash
claude mcp add mcp-server-for-revit -- "%AppData%\Autodesk\Revit\Addins\2025\revit_mcp_plugin\mcp-server-for-revit.exe"
```

Native AOT is tempting for startup time and size, but the MCP SDK's schema generation is
reflection-based; AOT requires explicit `WithTools<T>()` registration plus a source-generated
`JsonSerializerContext`. **Start with trimmed self-contained, not AOT.** Revisit if the binary size
becomes a complaint.

### 5.1 npm deprecation

Publish one final npm version whose README points at the new executable, then run
`npm deprecate mcp-server-for-revit "Replaced by the bundled C# server; see <link>"`. Keep the package
name reserved. Do not delete it — existing configs would break silently on the users' side.

---

## 6. Phased plan

| Phase | Work | Est. |
| --- | --- | --- |
| 0 | Decide: folder name, distribution option, whether to fix §7 bugs in-flight | — |
| 1 | Skeleton: csproj, `Program.cs`, stderr logging, `RevitClient` + `RevitConnection`, `say_hello` working end-to-end against a live Revit | 1 d |
| 2 | Port the remaining 22 passthrough tools + their schemas | 1.5–2 d |
| 3 | `Microsoft.Data.Sqlite` store + the 3 data tools; pick the new DB location | 0.5–1 d |
| 4 | Tests: fake TCP endpoint round-trip, schema snapshots, in-memory SQLite, tool-name ↔ `command.json` consistency check | 0.5–1 d |
| 5 | `release.yml` (`dotnet publish`, bundle exe into all 8 ZIPs), `release.ps1`, `README.md`, `.gitignore` | 1 d |
| 6 | Delete `server/`, deprecate the npm package | 0.5 d |
| | **Total** | **5–7 d** |

Phases 1–4 can land on a branch behind the existing Node server; only phase 5–6 is the user-visible
cutover. Consider one release that ships **both** servers so early adopters can switch back.

---

## 7. Risks, gotchas and pre-existing bugs

1. **The plugin's 8192-byte read.** `SocketService.HandleClientCommunication` deserialises whatever a
   single `stream.Read` returns. A request larger than 8 KB, or one split across TCP segments, yields
   a parse error. `send_code_to_revit` with a long snippet and `create_level` with a large `data`
   array can both exceed it. This bug exists today and the port does not change it. Fixing it properly
   means adding framing (newline-delimited or length-prefixed) on **both** sides — a plugin change, so
   out of scope for v1. Worth a follow-up issue.
2. **Reply framing by trial parse.** Both ends rely on "try to parse the buffer until it works". It is
   fragile but proven; reimplement it faithfully rather than improving it, or you will diverge from
   the plugin.
3. **The DB lives in the npx cache.** Porting is the natural moment to move it to
   `%LOCALAPPDATA%\mcp-servers-for-revit\revit-data.db`. Users with data in the old location will need
   a one-line note in the release notes; realistically almost nobody has, since the current location
   is wiped by `npm cache clean`.
4. **stdout is the protocol.** Any stray `Console.WriteLine` corrupts the stdio stream. Configure
   `logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)` in `Program.cs` and add a
   test or analyser rule against `Console.Write*` in the server project.
5. **MCP C# SDK version.** Pin the exact version so a minor upgrade cannot silently reshape the
   published tool schemas.
6. **Hard-coded `localhost:8080`** on both sides. Make it configurable in the new server
   (`REVIT_MCP_HOST` / `REVIT_MCP_PORT`) with the same defaults — costs nothing and unblocks anyone
   running Revit in a VM.
7. **Windows-only.** The Node server nominally ran anywhere; a `win-x64` self-contained exe does not.
   In practice the server is useless without Revit on the same machine, so this is a non-issue, but
   state it in the README.
8. **No existing server tests.** There is no safety net for the port. Capture the current tool
   schemas as JSON before starting and diff the C# output against them — that is the single highest-
   value hour in this whole plan.
9. **The 3 empty stub tools.** `modify_element`, `search_modules`, `use_module` are 0 bytes. Delete
   them; do not port placeholders.
10. **Chinese-language comments and log strings** in `SocketClient.ts`, `ConnectionManager.ts` and
    `register.ts`. Translate rather than transliterate during the port.

---

## 8. The payoff beyond "one language"

- **Kills a schema duplication.** `server/src/tools/create_level.ts` hand-maintains a zod schema whose
  fields must match `commandset/Models/Architecture/LevelInfo.cs` field for field. The same is true for
  rooms, grids, dimensions, walls and the rest. Once both sides are C#, a shared `netstandard2.0`
  DTO project lets the tool parameters *be* the command's model — one definition, compiler-enforced.
  **Do not attempt this during the port**; do a like-for-like port first, then extract shared DTOs as
  a follow-up. It is the strongest long-term argument for the change.
- **One toolchain.** Contributors need Visual Studio and nothing else; CI drops the Node setup and the
  npm publish job.
- **Lock-step versioning.** Today the npm package and the addin ZIP can drift; a tool added to the
  server can call a command the installed command set does not have. Shipping one artifact makes that
  impossible.
- **Enforceable tool/command consistency.** A trivial unit test can assert every `[McpServerTool]`
  name has a matching `commandName` in `command.json`. Nothing checks this today.

### Future option, not part of this scope

The MCP server could eventually be hosted **inside** the Revit add-in over HTTP, deleting the TCP
bridge and the separate process entirely. That is only possible once the server is C#, which is a
further reason to do this port. It is a bigger change — MCP availability becomes tied to Revit being
open, clients must support HTTP transport, and the add-in gains a listening socket — so it belongs in
its own proposal.

---

## 9. Decisions needed before phase 1

1. Folder: reuse `server/`, or move to `src/RevitMcpServer/`? (Recommend: reuse `server/`.)
2. Distribution: self-contained exe in the ZIP, .NET tool, or both? (Recommend: exe, optionally both.)
3. Add the server project to `mcp-servers-for-revit.sln` with 16 config mappings, or a separate
   solution? (Recommend: add to the sln.)
4. Ship one release with both servers, or a hard cutover? (Recommend: one overlapping release.)
5. Fix the 8 KB framing bug now (requires a coordinated plugin change) or file it as a follow-up?
   (Recommend: follow-up.)
