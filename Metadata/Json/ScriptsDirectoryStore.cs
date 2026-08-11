using System.Text.Json;

namespace DbMetaTool.Metadata.Json;

/// <summary>
/// Reads/writes a <see cref="MetadataModel"/> to a scripts-dir folder tree, one JSON file
/// per object, under domains/, tables/, procedures/ subfolders:
/// <code>
/// scripts-dir/
///   domains/D_PRODUCT_NAME.json
///   tables/STOCK_ITEMS.json
///   procedures/GET_FRIDGE_ITEMS.json
/// </code>
/// One file per object (named after the object) keeps re-exports git-diff friendly — a
/// changed procedure touches exactly one file instead of reshuffling a shared array file.
/// Not wired into BuildDatabase/ExportScripts/UpdateDatabase yet.
/// </summary>
public static class ScriptsDirectoryStore
{
    private const string DomainsFolder = "domains";
    private const string TablesFolder = "tables";
    private const string ProceduresFolder = "procedures";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static MetadataModel Load(string scriptsDirectory)
    {
        return new MetadataModel
        {
            Domains = LoadObjects<DomainDefinition>(scriptsDirectory, DomainsFolder),
            Tables = LoadObjects<TableDefinition>(scriptsDirectory, TablesFolder),
            Procedures = LoadObjects<ProcedureDefinition>(scriptsDirectory, ProceduresFolder)
        };
    }

    public static void Save(MetadataModel model, string scriptsDirectory)
    {
        SaveObjects(model.Domains, scriptsDirectory, DomainsFolder);
        SaveObjects(model.Tables, scriptsDirectory, TablesFolder);
        SaveObjects(model.Procedures, scriptsDirectory, ProceduresFolder);
    }

    private static List<T> LoadObjects<T>(string scriptsDirectory, string folderName)
        where T : INamedMetadataObject
    {
        var folder = Path.Combine(scriptsDirectory, folderName);
        if (!Directory.Exists(folder))
            return new List<T>();

        var items = new List<T>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            using var stream = File.OpenRead(file);
            var item = JsonSerializer.Deserialize<T>(stream, Options);
            if (item is not null)
                items.Add(item);
        }

        return items
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static void SaveObjects<T>(List<T> objects, string scriptsDirectory, string folderName)
        where T : INamedMetadataObject
    {
        var folder = Path.Combine(scriptsDirectory, folderName);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);

        foreach (var obj in objects)
        {
            var file = Path.Combine(folder, obj.Name + ".json");
            using var stream = File.Create(file);
            JsonSerializer.Serialize(stream, obj, Options);
        }
    }
}
