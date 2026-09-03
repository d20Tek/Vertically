using Microsoft.Data.Sqlite;

namespace IssueTracker.Persistence;

/// <summary>
/// Resolves the shared SQLite database location so every Issue Tracker sample host (API, CLI, Web)
/// points at the <em>same</em> physical <c>issues.db</c> file rather than a copy under each host's
/// own output directory. Connection strings may use the <c>{SharedDataDir}</c> token, which is
/// expanded to the repo-relative <c>samples/IssueTracker</c> folder discovered at runtime.
/// </summary>
public static class SharedDataPath
{
    /// <summary>The token, usable in an <c>appsettings.json</c> connection string, that expands to the shared data directory.</summary>
    public const string Token = "{SharedDataDir}";

    /// <summary>The name of the folder that anchors the shared data directory across all sample hosts.</summary>
    public const string AnchorFolderName = "IssueTracker";

    /// <summary>
    /// Expands the <see cref="Token"/> in a SQLite connection string to the shared data directory and
    /// normalizes the <c>Data Source</c> to an absolute path so all hosts resolve the same file.
    /// </summary>
    public static string ResolveConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var sharedDir = GetSharedDataDirectory();
        var expanded = connectionString.Replace(Token, sharedDir, StringComparison.Ordinal);

        var sqliteBuilder = new SqliteConnectionStringBuilder(expanded);
        if (!string.IsNullOrWhiteSpace(sqliteBuilder.DataSource) && !Path.IsPathRooted(sqliteBuilder.DataSource))
        {
            sqliteBuilder.DataSource = Path.GetFullPath(Path.Combine(sharedDir, sqliteBuilder.DataSource));
        }

        return sqliteBuilder.ToString();
    }

    /// <summary>
    /// Finds the shared <c>samples/IssueTracker</c> directory by walking up from the running host's
    /// base directory to the nearest <see cref="AnchorFolderName"/> folder. Falls back to the base
    /// directory when the anchor cannot be located (e.g. an unexpected deployment layout).
    /// </summary>
    public static string GetSharedDataDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (string.Equals(dir.Name, AnchorFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return dir.FullName;
            }
        }

        return AppContext.BaseDirectory;
    }
}
