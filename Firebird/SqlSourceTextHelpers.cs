namespace DbMetaTool.Firebird;

/// <summary>
/// Strips DDL keyword wrappers off RDB$*_SOURCE blob text to match the model's
/// "raw literal/expression, no keyword" convention (DomainDefinition.DefaultValue,
/// ColumnDefinition.DefaultValue, DomainDefinition.CheckExpression). Shared between
/// DomainReader and TableReader, which both read the "DEFAULT ..." form; procedure
/// parameters use a different "= ..." form (RDB$PROCEDURE_PARAMETERS.RDB$DEFAULT_SOURCE)
/// and are stripped separately in ProcedureReader.
/// </summary>
internal static class SqlSourceTextHelpers
{
    public static string? StripDefaultKeyword(string? source)
    {
        if (source is null)
            return null;

        var trimmed = source.Trim();
        if (trimmed.StartsWith("DEFAULT", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["DEFAULT".Length..].TrimStart();

        return trimmed;
    }

    public static string? StripCheckWrapper(string? source)
    {
        if (source is null)
            return null;

        var trimmed = source.Trim();
        if (trimmed.StartsWith("CHECK", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["CHECK".Length..].TrimStart();

        if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
            trimmed = trimmed[1..^1].Trim();

        return trimmed;
    }
}
