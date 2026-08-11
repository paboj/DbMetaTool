namespace DbMetaTool.Metadata;

/// <summary>
/// Input or output parameter of a procedure. <see cref="DefaultValue"/> is only ever set
/// for input parameters — Firebird's RETURNS(...) clause doesn't support defaults.
/// </summary>
public sealed class ProcedureParameter
{
    public string Name { get; set; } = "";

    /// <summary>SQL type text, e.g. "INTEGER", "VARCHAR(100)", "SMALLINT".</summary>
    public string DataType { get; set; } = "";

    /// <summary>1-based parameter position within its IN or OUT list.</summary>
    public int Position { get; set; }

    /// <summary>Inline default expression (e.g. "0" for "INCLUDE_OPENED SMALLINT = 0"); input-only.</summary>
    public string? DefaultValue { get; set; }
}
