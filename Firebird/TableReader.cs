using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Metadata;

namespace DbMetaTool.Firebird;

/// <summary>
/// Reads user tables and their columns via one joined query (RDB$RELATION_FIELDS x
/// RDB$FIELDS x RDB$RELATIONS), grouping rows by relation name. Excludes views
/// (RDB$VIEW_BLR IS NULL) — out of scope.
/// </summary>
public static class TableReader
{
    public static List<TableDefinition> ReadAll(FbConnection connection)
    {
        var tables = new List<TableDefinition>();
        var tablesByName = new Dictionary<string, TableDefinition>(StringComparer.Ordinal);

        using var cmd = new FbCommand(
            "SELECT RF.RDB$RELATION_NAME, RF.RDB$FIELD_NAME, RF.RDB$FIELD_SOURCE, RF.RDB$FIELD_POSITION, " +
            "RF.RDB$NULL_FLAG AS COL_NULL_FLAG, RF.RDB$DEFAULT_SOURCE AS COL_DEFAULT_SOURCE, " +
            "F.RDB$FIELD_TYPE, F.RDB$FIELD_SUB_TYPE, F.RDB$FIELD_LENGTH, F.RDB$FIELD_PRECISION, " +
            "F.RDB$FIELD_SCALE, F.RDB$CHARACTER_LENGTH, F.RDB$NULL_FLAG AS DOMAIN_NULL_FLAG " +
            "FROM RDB$RELATION_FIELDS RF " +
            "JOIN RDB$FIELDS F ON F.RDB$FIELD_NAME = RF.RDB$FIELD_SOURCE " +
            "JOIN RDB$RELATIONS R ON R.RDB$RELATION_NAME = RF.RDB$RELATION_NAME " +
            "WHERE R.RDB$SYSTEM_FLAG = 0 AND R.RDB$VIEW_BLR IS NULL " +
            "ORDER BY RF.RDB$RELATION_NAME, RF.RDB$FIELD_POSITION",
            connection);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var relationName = reader.GetTrimmedString(0);
            if (!tablesByName.TryGetValue(relationName, out var table))
            {
                table = new TableDefinition { Name = relationName };
                tablesByName[relationName] = table;
                tables.Add(table);
            }

            var fieldSource = reader.GetTrimmedString(2);
            var isImplicitDomain = fieldSource.StartsWith("RDB$", StringComparison.Ordinal);

            var typeInfo = FirebirdTypeMapper.Map(
                Convert.ToInt16(reader.GetValue(6)),
                reader.GetNullableInt16(7),
                reader.GetNullableInt32(8),
                reader.GetNullableInt32(9),
                reader.GetNullableInt16(10),
                reader.GetNullableInt32(11));

            var colNullFlag = reader.GetNullableInt16(4);
            var domainNullFlag = reader.GetNullableInt16(12);

            table.Columns.Add(new ColumnDefinition
            {
                Name = reader.GetTrimmedString(1),
                DomainName = isImplicitDomain ? null : fieldSource,
                InlineType = isImplicitDomain ? typeInfo.ToSqlText() : null,
                Nullable = colNullFlag != 1 && domainNullFlag != 1,
                DefaultValue = SqlSourceTextHelpers.StripDefaultKeyword(reader.GetNullableString(5)),
                Position = Convert.ToInt32(reader.GetValue(3)) + 1
            });
        }

        return tables;
    }
}
