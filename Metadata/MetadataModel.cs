namespace DbMetaTool.Metadata;

/// <summary>
/// Aggregate of all metadata objects. Always processed/emitted in this field order
/// (domains -> tables -> procedures) to satisfy Firebird's dependency order.
/// </summary>
public sealed class MetadataModel
{
    public List<DomainDefinition> Domains { get; set; } = new();
    public List<TableDefinition> Tables { get; set; } = new();
    public List<ProcedureDefinition> Procedures { get; set; } = new();
}
