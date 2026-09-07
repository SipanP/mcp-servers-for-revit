using System.Text.Json.Nodes;
using RevitMcpServer.Setup;

namespace RevitMcpServer.Tests;

/// <summary>
/// Covers the installation flow against a temporary directory tree standing in for a machine
/// with Revit and Claude Desktop installed.
/// </summary>
public class SetupTests
{
    [Test]
    public async Task Detects_revit_from_its_installation_directory()
    {
        using var machine = FakeMachine.Create(revitYears: [2024, 2026]);

        var detected = RevitInstallations.Detect(machine.Paths);

        await Assert.That(detected.Select(r => r.Year)).IsEquivalentTo(new[] { 2024, 2026 });
        await Assert.That(detected.All(r => r.RevitInstalled)).IsTrue();
    }

    [Test]
    public async Task Detects_a_version_where_only_the_addin_is_present()
    {
        using var machine = FakeMachine.Create(revitYears: []);
        machine.InstallPluginFiles(2025);

        var detected = RevitInstallations.Detect(machine.Paths);

        await Assert.That(detected.Single().Year).IsEqualTo(2025);
        await Assert.That(detected.Single().RevitInstalled).IsFalse();
        await Assert.That(detected.Single().PluginInstalled).IsTrue();
    }

    [Test]
    public async Task Enables_every_command_from_the_command_set_manifest()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);

        var exitCode = machine.Run("setup");

        await Assert.That(exitCode).IsEqualTo(0);
        var registry = machine.CommandRegistry(2025);
        var commands = registry["commands"]!.AsArray();
        await Assert.That(commands.Count).IsEqualTo(machine.ManifestCommandCount);
        await Assert.That(commands.All(c => c!["enabled"]!.GetValue<bool>())).IsTrue();
    }

    [Test]
    public async Task Writes_an_assembly_path_the_plugin_can_resolve()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);

        machine.Run("setup");

        // The plugin joins this onto its Commands directory and substitutes the running version.
        var first = machine.CommandRegistry(2025)["commands"]!.AsArray()[0]!;
        await Assert.That(first["assemblyPath"]!.GetValue<string>())
            .IsEqualTo(@"RevitMCPCommandSet\{VERSION}\RevitMCPCommandSet.dll");
        await Assert.That(first["supportedRevitVersions"]!.AsArray()[0]!.GetValue<string>()).IsEqualTo("2025");
    }

    [Test]
    public async Task Adds_the_server_without_disturbing_other_mcp_servers()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        machine.WriteClaudeDesktopConfig("""
            {
              "mcpServers": { "filesystem": { "command": "npx", "args": ["-y", "server-filesystem"] } },
              "theme": "dark"
            }
            """);

        machine.Run("setup");

        var config = machine.ClaudeDesktopConfig();
        await Assert.That(config["mcpServers"]!["filesystem"]!["command"]!.GetValue<string>()).IsEqualTo("npx");
        await Assert.That(config["theme"]!.GetValue<string>()).IsEqualTo("dark");
        await Assert.That(config["mcpServers"]!["mcp-server-for-revit"]!["command"]!.GetValue<string>())
            .IsEqualTo(machine.ServerPath);
    }

    [Test]
    public async Task Creates_the_claude_desktop_config_when_there_is_none()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);

        machine.Run("setup");

        await Assert.That(File.Exists(machine.Paths.ClaudeDesktopConfigFile)).IsTrue();
        await Assert.That(machine.ClaudeDesktopConfig()["mcpServers"]!["mcp-server-for-revit"]).IsNotNull();
    }

    [Test]
    public async Task Keeps_a_backup_and_recovers_when_the_config_is_not_valid_json()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        machine.WriteClaudeDesktopConfig("{ this is not json");

        machine.Run("setup");

        await Assert.That(machine.ClaudeDesktopConfig()["mcpServers"]!["mcp-server-for-revit"]).IsNotNull();
        await Assert.That(File.Exists(machine.Paths.ClaudeDesktopConfigFile + ".backup")).IsTrue();
    }

    [Test]
    public async Task Running_setup_twice_changes_nothing_the_second_time()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);

        machine.Run("setup");
        var afterFirst = File.ReadAllText(machine.Paths.ClaudeDesktopConfigFile);
        machine.Run("setup");
        var afterSecond = File.ReadAllText(machine.Paths.ClaudeDesktopConfigFile);

        await Assert.That(afterSecond).IsEqualTo(afterFirst);
    }

    [Test]
    public async Task Only_sets_up_the_requested_revit_versions()
    {
        using var machine = FakeMachine.Create(revitYears: [2024, 2025]);
        machine.InstallPluginFiles(2024);
        machine.InstallPluginFiles(2025);

        machine.Run("setup", "--revit", "2025");

        await Assert.That(File.Exists(machine.Installation(2025).CommandRegistryPath)).IsTrue();
        await Assert.That(File.Exists(machine.Installation(2024).CommandRegistryPath)).IsFalse();
    }

    [Test]
    public async Task Reports_a_problem_when_the_plugin_files_are_missing()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);

        var exitCode = machine.Run("setup", out var output);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("Plugin files are missing");
    }

    [Test]
    public async Task Uninstall_removes_only_this_server()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        machine.WriteClaudeDesktopConfig("""
            { "mcpServers": { "filesystem": { "command": "npx" } } }
            """);
        machine.Run("setup");

        machine.Run("uninstall");

        var servers = machine.ClaudeDesktopConfig()["mcpServers"]!.AsObject();
        await Assert.That(servers.ContainsKey("mcp-server-for-revit")).IsFalse();
        await Assert.That(servers.ContainsKey("filesystem")).IsTrue();
    }

    [Test]
    public async Task Doctor_passes_on_a_complete_installation()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        machine.Run("setup");

        var exitCode = machine.Run("doctor", out var output);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Everything looks correct.");
    }

    [Test]
    public async Task Doctor_names_what_is_wrong_when_commands_were_never_enabled()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);

        var exitCode = machine.Run("doctor", out var output);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("no commands enabled");
        await Assert.That(output).Contains("setup");
    }

    [Test]
    public async Task Doctor_flags_a_client_pointing_at_a_missing_executable()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        machine.Run("setup");
        machine.WriteClaudeDesktopConfig("""
            { "mcpServers": { "mcp-server-for-revit": { "command": "C:\\gone\\mcp-server-for-revit.exe" } } }
            """);

        machine.Run("doctor", out var output);

        await Assert.That(output).Contains("points at a missing executable");
    }

    [Test]
    public async Task Registers_with_claude_code_through_its_cli()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        var runner = new RecordingProcessRunner();

        machine.Run(runner, "setup");

        await Assert.That(runner.Invocations).Contains(invocation =>
            invocation.StartsWith("claude mcp add mcp-server-for-revit --scope user --"));
    }

    [Test]
    public async Task Says_what_to_run_by_hand_when_the_claude_code_cli_fails()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        var runner = new RecordingProcessRunner { Result = new ProcessResult(true, 1, "boom") };

        machine.Run(runner, "setup", out var output);

        await Assert.That(output).Contains("could not register automatically");
        await Assert.That(output).Contains("claude mcp add mcp-server-for-revit");
    }

    [Test]
    public async Task Treats_a_missing_claude_code_cli_as_normal()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);
        machine.InstallPluginFiles(2025);
        var runner = new RecordingProcessRunner { Result = ProcessResult.NotFound };

        var exitCode = machine.Run(runner, "setup", out var output);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Claude Code: not installed, skipped");
    }

    [Test]
    public async Task Rejects_an_unknown_option_rather_than_guessing()
    {
        using var machine = FakeMachine.Create(revitYears: [2025]);

        var exitCode = machine.RunRaw(["setup", "--nonsense"], out var output);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output).Contains("Unknown option");
    }
}
