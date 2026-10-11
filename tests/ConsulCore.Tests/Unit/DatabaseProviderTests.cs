using consulcore_backend.Data;

namespace ConsulCore.Tests.Unit;

public class DatabaseProviderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "consulcore-dbp-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sqlite")]
    [InlineData("sqlite")]
    public void Sqlite_or_unset_provider_is_accepted(string? provider)
    {
        var result = DatabaseProvider.ResolveConnectionString(provider, "Data Source=a.db", _root);

        Assert.Contains(Path.Combine(_root, "a.db"), result);
    }

    [Theory]
    [InlineData("MySql")]
    [InlineData("SqlServer")]
    public void Provider_not_enabled_yet_fails_fast_with_pointer_to_docs(string provider)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => DatabaseProvider.ResolveConnectionString(provider, "Data Source=a.db", _root));

        Assert.Contains("docs/database.md", ex.Message);
    }

    [Fact]
    public void Relative_path_resolves_against_content_root_and_creates_directory()
    {
        var result = DatabaseProvider.ResolveConnectionString(null, "Data Source=App_Data/x.db", _root);

        Assert.Contains(Path.Combine(_root, "App_Data", "x.db"), result);
        Assert.True(Directory.Exists(Path.Combine(_root, "App_Data")));
    }

    [Fact]
    public void Absolute_path_is_kept()
    {
        var absolute = Path.Combine(_root, "abs", "y.db");

        var result = DatabaseProvider.ResolveConnectionString(null, $"Data Source={absolute}", _root);

        Assert.Contains(absolute, result);
    }

    [Fact]
    public void In_memory_database_is_not_treated_as_a_path()
    {
        var result = DatabaseProvider.ResolveConnectionString(null, "Data Source=:memory:", _root);

        Assert.Contains(":memory:", result);
        Assert.False(Directory.Exists(_root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_connection_string_returns_empty_so_no_database_is_configured(string? connectionString)
    {
        Assert.Equal(string.Empty, DatabaseProvider.ResolveConnectionString(null, connectionString, _root));
    }
}
