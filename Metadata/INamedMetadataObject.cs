namespace DbMetaTool.Metadata;

/// <summary>Implemented by the top-level metadata object types so ScriptsDirectoryStore can sort/name-file them without reflection.</summary>
public interface INamedMetadataObject
{
    string Name { get; }
}
