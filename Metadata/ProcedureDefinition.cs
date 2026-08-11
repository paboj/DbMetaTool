namespace DbMetaTool.Metadata;

/// <summary>
/// CREATE OR ALTER PROCEDURE definition. SourceBody holds only the BEGIN...END body text;
/// the CREATE header is reconstructed from Name/InputParameters/OutputParameters so there's
/// a single source of truth for the parameter list.
/// </summary>
public sealed class ProcedureDefinition : INamedMetadataObject
{
    public string Name { get; set; } = "";
    public List<ProcedureParameter> InputParameters { get; set; } = new();
    public List<ProcedureParameter> OutputParameters { get; set; } = new();

    /// <summary>Procedure body text, e.g. "BEGIN\n  ...\nEND".</summary>
    public string SourceBody { get; set; } = "";
}
