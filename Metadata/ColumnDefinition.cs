namespace DbMetaTool.Metadata;

/// <summary>
/// A table column. Exactly one of <see cref="DomainName"/> or <see cref="InlineType"/>
/// should be set (mutually exclusive) — by convention only, not enforced at runtime.
/// </summary>
public sealed class ColumnDefinition
{
    public string Name { get; set; } = "";

    /// <summary>Referenced domain name (e.g. "D_PRODUCT_NAME"), or null if InlineType is used.</summary>
    public string? DomainName { get; set; }

    /// <summary>Inline SQL type text (e.g. "INTEGER", "DATE"), or null if DomainName is used.</summary>
    public string? InlineType { get; set; }

    public bool Nullable { get; set; } = true;
    public string? DefaultValue { get; set; }

    /// <summary>1-based column position.</summary>
    public int Position { get; set; }
}
