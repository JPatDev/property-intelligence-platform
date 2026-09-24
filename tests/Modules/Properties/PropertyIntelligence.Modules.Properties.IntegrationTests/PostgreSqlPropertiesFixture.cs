using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Properties.IntegrationTests;

public sealed partial class PostgreSqlPropertiesFixture : IAsyncLifetime
{
    private readonly string _databaseName =
        $"property_intelligence_properties_tests_{Guid.NewGuid():N}";
    private string _adminConnectionString = string.Empty;
    private ServiceProvider? _serviceProvider;
    private AsyncServiceScope _scope;
    private bool _scopeCreated;
    private bool _databaseCreated;

    public bool IsAvailable { get; private set; }

    public string UnavailableReason { get; private set; } =
        "The PostgreSQL/PostGIS test fixture is unavailable.";

    public IServiceProvider Services =>
        _scope.ServiceProvider ??
        throw new InvalidOperationException("The PostgreSQL fixture has not been initialized.");

    public async ValueTask InitializeAsync()
    {
        ValidateDatabaseName(_databaseName);
        _adminConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__PropertiesTestAdmin")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__WorkflowTestAdmin")
            ?? "Host=localhost;Port=5432;Database=property_intelligence;Username=postgres;Pooling=false";
        var adminBuilder = new NpgsqlConnectionStringBuilder(_adminConnectionString);
        if (string.IsNullOrEmpty(adminBuilder.Password))
        {
            adminBuilder.Password = ReadPasswordFile(adminBuilder);
            _adminConnectionString = adminBuilder.ConnectionString;
        }

        await using (var adminConnection = new NpgsqlConnection(_adminConnectionString))
        {
            await adminConnection.OpenAsync();
            await using (var extensionCommand = adminConnection.CreateCommand())
            {
                extensionCommand.CommandText =
                    "SELECT EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'postgis')";
                var postgisAvailable = (bool)(await extensionCommand.ExecuteScalarAsync() ?? false);
                if (!postgisAvailable)
                {
                    UnavailableReason =
                        "PostGIS is not installed on the configured PostgreSQL test server.";
                    return;
                }
            }

            await using var command = adminConnection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\" TEMPLATE template0";
            await command.ExecuteNonQueryAsync();
            _databaseCreated = true;
        }

        var testConnection = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName,
            Pooling = false,
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Properties"] = testConnection.ConnectionString,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        new PropertiesModule().AddServices(services, configuration);
        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        _scope = _serviceProvider.CreateAsyncScope();
        _scopeCreated = true;

        await Services.GetRequiredService<PropertiesDbContext>().Database.MigrateAsync();
        IsAvailable = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_scopeCreated)
        {
            await _scope.DisposeAsync();
        }

        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        if (!_databaseCreated)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        ValidateDatabaseName(_databaseName);
        await using var adminConnection = new NpgsqlConnection(_adminConnectionString);
        await adminConnection.OpenAsync();
        await using (var terminateCommand = adminConnection.CreateCommand())
        {
            terminateCommand.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @database_name
                  AND pid <> pg_backend_pid()
                """;
            terminateCommand.Parameters.AddWithValue("database_name", _databaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using var dropCommand = adminConnection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE \"{_databaseName}\"";
        await dropCommand.ExecuteNonQueryAsync();
    }

    private static void ValidateDatabaseName(string databaseName)
    {
        if (!DatabaseNamePattern().IsMatch(databaseName))
        {
            throw new InvalidOperationException(
                $"Refusing to create or drop unexpected database '{databaseName}'.");
        }
    }

    private static string ReadPasswordFile(NpgsqlConnectionStringBuilder connection)
    {
        var passfilePath = Environment.GetEnvironmentVariable("PGPASSFILE");
        if (string.IsNullOrWhiteSpace(passfilePath))
        {
            passfilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "postgresql",
                "pgpass.conf");
        }

        if (!File.Exists(passfilePath))
        {
            throw new InvalidOperationException(
                "PostgreSQL test credentials were not found. Configure " +
                "ConnectionStrings__PropertiesTestAdmin or pgpass.conf.");
        }

        foreach (var line in File.ReadLines(passfilePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var fields = SplitPasswordFileLine(line);
            if (fields.Count == 5 &&
                Matches(fields[0], connection.Host ?? "localhost") &&
                Matches(fields[1], connection.Port.ToString()) &&
                Matches(fields[2], connection.Database ?? "property_intelligence") &&
                Matches(fields[3], connection.Username ?? "postgres"))
            {
                return fields[4];
            }
        }

        throw new InvalidOperationException(
            "No pgpass.conf entry matches the PostgreSQL properties test connection.");
    }

    private static IReadOnlyList<string> SplitPasswordFileLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var escaped = false;
        foreach (var character in line)
        {
            if (escaped)
            {
                current.Append(character);
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else if (character == ':')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        if (escaped)
        {
            current.Append('\\');
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static bool Matches(string configured, string actual) =>
        configured == "*" ||
        string.Equals(configured, actual, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^property_intelligence_properties_tests_[a-f0-9]{32}$")]
    private static partial Regex DatabaseNamePattern();
}
