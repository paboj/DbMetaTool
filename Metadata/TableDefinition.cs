namespace DbMetaTool.Metadata;

public sealed class TableDefinition : INamedMetadataObject
{
    public string Name { get; set; } = "";
    public List<ColumnDefinition> Columns { get; set; } = new();
}
