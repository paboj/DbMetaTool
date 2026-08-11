namespace DbMetaTool.Firebird;

/// <summary>
/// Host/port/credentials/charset are hardcoded here rather than passed as parameters —
/// BuildDatabase's signature (databaseDirectory, scriptsDirectory) has no room for a
/// connection string, so per dev-environment.md this is the one intentional exception to
/// "no hardcoded config" in this project.
/// </summary>
public static class FirebirdConnectionFactory
{
    public const string Host = "localhost";
    public const int Port = 3050;
    public const string User = "SYSDBA";
    public const string Password = "masterkey";

    /// <summary>Matches manual-test.fdb's RDB$CHARACTER_SET_NAME (verified 2026-08-11) so built databases stay structurally comparable.</summary>
    public const string Charset = "UTF8";

    public static string BuildConnectionString(string databasePath) =>
        $"User={User};Password={Password};DataSource={Host};Port={Port};" +
        $"Database={databasePath};Dialect=3;Charset={Charset};";
}
