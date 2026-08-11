using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

/// <summary>Null-safe column readers shared by the RDB$ metadata readers.</summary>
internal static class FbDataReaderExtensions
{
    public static string GetTrimmedString(this FbDataReader reader, int ordinal) =>
        reader.GetString(ordinal).Trim();

    public static string? GetNullableString(this FbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    public static short? GetNullableInt16(this FbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt16(reader.GetValue(ordinal));

    public static int? GetNullableInt32(this FbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
}
