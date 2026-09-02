using RevitMcpServer.Data;

namespace RevitMcpServer.Tests;

/// <summary>Covers the SQLite store ported from the previous server's database module.</summary>
public class RevitDataStoreTests
{
    private static RevitDataStore NewStore() =>
        new(Path.Combine(Path.GetTempPath(), $"revit-mcp-tests-{Guid.NewGuid():N}", "revit-data.db"));

    [Test]
    public async Task Stores_a_project_and_reads_it_back_by_name()
    {
        var store = NewStore();

        var id = store.StoreProject(new ProjectRecord { ProjectName = "Tower", ProjectNumber = "P-1" });
        var project = store.GetProjectByName("Tower");

        await Assert.That(id).IsGreaterThan(0);
        await Assert.That(project).IsNotNull();
        await Assert.That(project!["project_number"]).IsEqualTo("P-1");
    }

    [Test]
    public async Task Updates_an_existing_project_rather_than_inserting_a_duplicate()
    {
        var store = NewStore();

        var first = store.StoreProject(new ProjectRecord { ProjectName = "Tower", ClientName = "Acme" });
        var second = store.StoreProject(new ProjectRecord { ProjectName = "Tower", ClientName = "Globex" });

        await Assert.That(second).IsEqualTo(first);
        await Assert.That(store.GetAllProjects().Count).IsEqualTo(1);
        await Assert.That(store.GetProjectByName("Tower")!["client_name"]).IsEqualTo("Globex");
    }

    [Test]
    public async Task Renders_timestamps_as_iso_8601_and_metadata_as_an_object()
    {
        var store = NewStore();
        store.StoreProject(new ProjectRecord
        {
            ProjectName = "Tower",
            Metadata = new Dictionary<string, string> { ["phase"] = "concept" }
        });

        var project = store.GetProjectByName("Tower")!;

        await Assert.That((string)project["timestamp"]!).EndsWith("Z");
        await Assert.That(project["metadata"]!.ToString()).Contains("concept");
    }

    [Test]
    public async Task Stores_rooms_against_a_project_and_replaces_them_on_restore()
    {
        var store = NewStore();
        var projectId = store.StoreProject(new ProjectRecord { ProjectName = "Tower" });

        var stored = store.StoreRooms(projectId, [
            new RoomRecord { RoomId = "101", RoomName = "Lobby", Area = 42.5 },
            new RoomRecord { RoomId = "102", RoomName = "Kitchen" }
        ]);
        store.StoreRooms(projectId, [new RoomRecord { RoomId = "101", RoomName = "Reception" }]);

        var rooms = store.GetRoomsByProjectId(projectId);
        await Assert.That(stored).IsEqualTo(2);
        await Assert.That(rooms.Count).IsEqualTo(2);
        await Assert.That(rooms.Single(r => (string)r["room_id"]! == "101")["room_name"]).IsEqualTo("Reception");
    }

    [Test]
    public async Task Deletes_rooms_along_with_their_project()
    {
        var store = NewStore();
        var projectId = store.StoreProject(new ProjectRecord { ProjectName = "Tower" });
        store.StoreRooms(projectId, [new RoomRecord { RoomId = "101" }]);

        await Assert.That(store.GetAllRoomsWithProject().Count).IsEqualTo(1);
        await Assert.That(store.GetStats()["total_rooms"]).IsEqualTo(1L);
        await Assert.That(store.GetStats()["total_projects"]).IsEqualTo(1L);
    }

    [Test]
    public async Task Returns_null_for_an_unknown_project()
    {
        var store = NewStore();

        await Assert.That(store.GetProjectByName("nope")).IsNull();
        await Assert.That(store.GetProjectById(9999)).IsNull();
    }
}
