namespace DbMetaTool.Metadata;

/// <summary>CREATE DOMAIN definition.</summary>
public sealed class DomainDefinition : INamedMetadataObject
{
    public string Name { get; set; } = "";

    /// <summary>SQL base type keyword, e.g. "VARCHAR", "SMALLINT", "DOUBLE PRECISION", "DATE".</summary>
    public string BaseType { get; set; } = "";

    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }

    public bool Nullable { get; set; } = true;

    /// <summary>Literal SQL default expression text, e.g. "0". Null = no DEFAULT clause.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Raw CHECK expression text without the CHECK(...) wrapper, e.g. "VALUE >= 0".</summary>
    public string? CheckExpression { get; set; }
}
