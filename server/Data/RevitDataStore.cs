using System.Data;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace RevitMcpServer.Data;

/// <summary>
/// Local SQLite store for Revit project and room metadata.
/// Schema and query shapes are a direct port of the previous <c>server/src/database/</c> module.
/// </summary>
public sealed class RevitDataStore
{
    private readonly string _connectionString;

    public RevitDataStore()
        : this(DefaultDatabasePath())
    {
    }

    public RevitDataStore(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        Initialize();
    }

    /// <summary>
    /// The previous server stored the database next to its own build output, which under
    /// <c>npx</c> meant inside the npm cache — wiped by <c>npm cache clean</c>. Per-user
    /// application data is the durable equivalent.
    /// </summary>
    public static string DefaultDatabasePath()
    {
        var root = Environment.GetEnvironmentVariable("REVIT_MCP_DATABASEPATH");
        if (!string.IsNullOrWhiteSpace(root))
        {
            return root;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(appData))
        {
            appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(appData, "mcp-servers-for-revit", "revit-data.db");
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    private void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS projects (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              project_name TEXT NOT NULL,
              project_path TEXT,
              project_number TEXT,
              project_address TEXT,
              client_name TEXT,
              project_status TEXT,
              author TEXT,
              timestamp INTEGER NOT NULL,
              last_updated INTEGER NOT NULL,
              metadata TEXT
            );

            CREATE TABLE IF NOT EXISTS rooms (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              project_id INTEGER NOT NULL,
              room_id TEXT NOT NULL,
              room_name TEXT,
              room_number TEXT,
              department TEXT,
              level TEXT,
              area REAL,
              perimeter REAL,
              occupancy TEXT,
              comments TEXT,
              timestamp INTEGER NOT NULL,
              metadata TEXT,
              FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
              UNIQUE(project_id, room_id)
            );

            CREATE INDEX IF NOT EXISTS idx_projects_name ON projects(project_name);
            CREATE INDEX IF NOT EXISTS idx_projects_timestamp ON projects(timestamp);
            CREATE INDEX IF NOT EXISTS idx_rooms_project_id ON rooms(project_id);
            CREATE INDEX IF NOT EXISTS idx_rooms_room_number ON rooms(room_number);
            """;
        command.ExecuteNonQuery();
    }

    public long StoreProject(ProjectRecord project)
    {
        using var connection = Open();
        return StoreProject(connection, project);
    }

    private static long StoreProject(SqliteConnection connection, ProjectRecord project)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var metadata = SerializeMetadata(project.Metadata);

        var existingId = ScalarLong(connection, "SELECT id FROM projects WHERE project_name = $name;",
            ("$name", project.ProjectName));

        if (existingId is { } id)
        {
            Execute(connection,
                """
                UPDATE projects SET
                  project_path = $path,
                  project_number = $number,
                  project_address = $address,
                  client_name = $client,
                  project_status = $status,
                  author = $author,
                  last_updated = $updated,
                  metadata = $metadata
                WHERE id = $id;
                """,
                ("$path", project.ProjectPath),
                ("$number", project.ProjectNumber),
                ("$address", project.ProjectAddress),
                ("$client", project.ClientName),
                ("$status", project.ProjectStatus),
                ("$author", project.Author),
                ("$updated", timestamp),
                ("$metadata", metadata),
                ("$id", id));
            return id;
        }

        Execute(connection,
            """
            INSERT INTO projects (
              project_name, project_path, project_number, project_address,
              client_name, project_status, author, timestamp, last_updated, metadata
            ) VALUES ($name, $path, $number, $address, $client, $status, $author, $created, $updated, $metadata);
            """,
            ("$name", project.ProjectName),
            ("$path", project.ProjectPath),
            ("$number", project.ProjectNumber),
            ("$address", project.ProjectAddress),
            ("$client", project.ClientName),
            ("$status", project.ProjectStatus),
            ("$author", project.Author),
            ("$created", timestamp),
            ("$updated", timestamp),
            ("$metadata", metadata));

        return ScalarLong(connection, "SELECT last_insert_rowid();") ?? 0;
    }

    /// <summary>Stores rooms in one transaction and returns how many were written.</summary>
    public int StoreRooms(long projectId, IReadOnlyList<RoomRecord> rooms)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();

        var count = 0;
        foreach (var room in rooms)
        {
            StoreRoom(connection, transaction, projectId, room);
            count++;
        }

        transaction.Commit();
        return count;
    }

    private static void StoreRoom(SqliteConnection connection, SqliteTransaction transaction, long projectId, RoomRecord room)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var metadata = SerializeMetadata(room.Metadata);

        var existingId = ScalarLong(connection,
            "SELECT id FROM rooms WHERE project_id = $projectId AND room_id = $roomId;",
            transaction,
            ("$projectId", projectId), ("$roomId", room.RoomId));

        if (existingId is { } id)
        {
            Execute(connection,
                """
                UPDATE rooms SET
                  room_name = $name, room_number = $number, department = $department,
                  level = $level, area = $area, perimeter = $perimeter,
                  occupancy = $occupancy, comments = $comments,
                  timestamp = $timestamp, metadata = $metadata
                WHERE id = $id;
                """,
                transaction,
                ("$name", room.RoomName), ("$number", room.RoomNumber), ("$department", room.Department),
                ("$level", room.Level), ("$area", room.Area), ("$perimeter", room.Perimeter),
                ("$occupancy", room.Occupancy), ("$comments", room.Comments),
                ("$timestamp", timestamp), ("$metadata", metadata), ("$id", id));
            return;
        }

        Execute(connection,
            """
            INSERT INTO rooms (
              project_id, room_id, room_name, room_number, department,
              level, area, perimeter, occupancy, comments, timestamp, metadata
            ) VALUES ($projectId, $roomId, $name, $number, $department,
                      $level, $area, $perimeter, $occupancy, $comments, $timestamp, $metadata);
            """,
            transaction,
            ("$projectId", projectId), ("$roomId", room.RoomId), ("$name", room.RoomName),
            ("$number", room.RoomNumber), ("$department", room.Department), ("$level", room.Level),
            ("$area", room.Area), ("$perimeter", room.Perimeter), ("$occupancy", room.Occupancy),
            ("$comments", room.Comments), ("$timestamp", timestamp), ("$metadata", metadata));
    }

    public IReadOnlyList<Dictionary<string, object?>> GetAllProjects() =>
        Query(
            """
            SELECT id, project_name, project_path, project_number, project_address,
                   client_name, project_status, author, timestamp, last_updated, metadata
            FROM projects
            ORDER BY last_updated DESC;
            """);

    public Dictionary<string, object?>? GetProjectById(long projectId) =>
        Query(
            """
            SELECT id, project_name, project_path, project_number, project_address,
                   client_name, project_status, author, timestamp, last_updated, metadata
            FROM projects
            WHERE id = $id;
            """,
            ("$id", projectId)).FirstOrDefault();

    public Dictionary<string, object?>? GetProjectByName(string projectName) =>
        Query(
            """
            SELECT id, project_name, project_path, project_number, project_address,
                   client_name, project_status, author, timestamp, last_updated, metadata
            FROM projects
            WHERE project_name = $name;
            """,
            ("$name", projectName)).FirstOrDefault();

    public IReadOnlyList<Dictionary<string, object?>> GetRoomsByProjectId(long projectId) =>
        Query(
            """
            SELECT id, project_id, room_id, room_name, room_number, department,
                   level, area, perimeter, occupancy, comments, timestamp, metadata
            FROM rooms
            WHERE project_id = $id
            ORDER BY room_number;
            """,
            ("$id", projectId));

    public IReadOnlyList<Dictionary<string, object?>> GetAllRoomsWithProject() =>
        Query(
            """
            SELECT r.id, r.project_id, r.room_id, r.room_name, r.room_number,
                   r.department, r.level, r.area, r.perimeter, r.occupancy,
                   r.comments, r.timestamp, r.metadata,
                   p.project_name, p.project_number
            FROM rooms r
            JOIN projects p ON r.project_id = p.id
            ORDER BY p.project_name, r.room_number;
            """);

    public Dictionary<string, object?> GetStats()
    {
        using var connection = Open();
        return new Dictionary<string, object?>
        {
            ["total_projects"] = ScalarLong(connection, "SELECT COUNT(*) FROM projects;") ?? 0,
            ["total_rooms"] = ScalarLong(connection, "SELECT COUNT(*) FROM rooms;") ?? 0
        };
    }

    /// <summary>
    /// Reads rows into dictionaries, expanding <c>metadata</c> back into an object and rendering
    /// the millisecond timestamps as ISO-8601 — matching the previous server's response shape.
    /// </summary>
    private IReadOnlyList<Dictionary<string, object?>> Query(
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);

        var rows = new List<Dictionary<string, object?>>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                row[name] = name switch
                {
                    "metadata" => reader.IsDBNull(i) ? null : ParseMetadata(reader.GetString(i)),
                    "timestamp" or "last_updated" => reader.IsDBNull(i)
                        ? null
                        : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(i)).UtcDateTime
                            .ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    _ => reader.IsDBNull(i) ? null : reader.GetValue(i)
                };
            }

            rows.Add(row);
        }

        return rows;
    }

    private static JsonElement? ParseMetadata(string raw)
    {
        try
        {
            return JsonDocument.Parse(raw).RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, string>? metadata) =>
        metadata is null ? null : JsonSerializer.Serialize(metadata);

    private static void Execute(
        SqliteConnection connection,
        string sql,
        params (string Name, object? Value)[] parameters) =>
        Execute(connection, sql, null, parameters);

    private static void Execute(
        SqliteConnection connection,
        string sql,
        SqliteTransaction? transaction,
        params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        AddParameters(command, parameters);
        command.ExecuteNonQuery();
    }

    private static long? ScalarLong(
        SqliteConnection connection,
        string sql,
        params (string Name, object? Value)[] parameters) =>
        ScalarLong(connection, sql, null, parameters);

    private static long? ScalarLong(
        SqliteConnection connection,
        string sql,
        SqliteTransaction? transaction,
        params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        AddParameters(command, parameters);
        var value = command.ExecuteScalar();
        return value is null or DBNull ? null : Convert.ToInt64(value);
    }

    private static void AddParameters(IDbCommand command, (string Name, object? Value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
