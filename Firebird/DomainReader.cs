using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Metadata;

namespace DbMetaTool.Firebird;

/// <summary>
/// Reads user-created domains from RDB$FIELDS. RDB$SYSTEM_FLAG = 0 alone is not enough —
/// Firebird also creates implicit domains (named "RDB$&lt;n&gt;") for every inline-typed
/// column/parameter, and those get RDB$SYSTEM_FLAG = 0 too. Real domain names never start
/// with "RDB$" (reserved), so that name check is what actually separates the two.
/// </summary>
public static class DomainReader
{
    public static List<DomainDefinition> ReadAll(FbConnection connection)
    {
        var domains = new List<DomainDefinition>();

        using var cmd = new FbCommand(
            "SELECT RDB$FIELD_NAME, RDB$FIELD_TYPE, RDB$FIELD_SUB_TYPE, RDB$FIELD_LENGTH, " +
            "RDB$FIELD_PRECISION, RDB$FIELD_SCALE, RDB$CHARACTER_LENGTH, " +
            "RDB$NULL_FLAG, RDB$DEFAULT_SOURCE, RDB$VALIDATION_SOURCE " +
            "FROM RDB$FIELDS " +
            "WHERE RDB$SYSTEM_FLAG = 0 AND RDB$FIELD_NAME NOT LIKE 'RDB$%' " +
            "ORDER BY RDB$FIELD_NAME",
            connection);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var typeInfo = FirebirdTypeMapper.Map(
                Convert.ToInt16(reader.GetValue(1)),
                reader.GetNullableInt16(2),
                reader.GetNullableInt32(3),
                reader.GetNullableInt32(4),
                reader.GetNullableInt16(5),
                reader.GetNullableInt32(6));

            var nullFlag = reader.GetNullableInt16(7);

            domains.Add(new DomainDefinition
            {
                Name = reader.GetTrimmedString(0),
                BaseType = typeInfo.Keyword,
                Length = typeInfo.Length,
                Precision = typeInfo.Precision,
                Scale = typeInfo.Scale,
                Nullable = nullFlag != 1,
                DefaultValue = SqlSourceTextHelpers.StripDefaultKeyword(reader.GetNullableString(8)),
                CheckExpression = SqlSourceTextHelpers.StripCheckWrapper(reader.GetNullableString(9))
            });
        }

        return domains;
    }
}
