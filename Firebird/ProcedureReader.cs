using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Metadata;

namespace DbMetaTool.Firebird;

/// <summary>
/// Reads user procedures (source body) and their parameters. RDB$PROCEDURE_PARAMETERS.
/// RDB$PARAMETER_NUMBER is 0-based and numbered separately per direction (RDB$PARAMETER_TYPE:
/// 0=input, 1=output); RDB$FIELD_SOURCE always resolves to a domain (real or Firebird-implicit)
/// via RDB$FIELDS, regardless of whether the parameter was declared inline.
/// </summary>
public static class ProcedureReader
{
    public static List<ProcedureDefinition> ReadAll(FbConnection connection)
    {
        var procedures = ReadProcedureHeaders(connection);
        AttachParameters(connection, procedures);
        return procedures.Values.ToList();
    }

    private static Dictionary<string, ProcedureDefinition> ReadProcedureHeaders(FbConnection connection)
    {
        var procedures = new Dictionary<string, ProcedureDefinition>(StringComparer.Ordinal);

        using var cmd = new FbCommand(
            "SELECT RDB$PROCEDURE_NAME, RDB$PROCEDURE_SOURCE FROM RDB$PROCEDURES " +
            "WHERE RDB$SYSTEM_FLAG = 0 ORDER BY RDB$PROCEDURE_NAME",
            connection);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetTrimmedString(0);
            procedures[name] = new ProcedureDefinition
            {
                Name = name,
                SourceBody = reader.GetNullableString(1)?.Trim() ?? ""
            };
        }

        return procedures;
    }

    private static void AttachParameters(FbConnection connection, Dictionary<string, ProcedureDefinition> procedures)
    {
        using var cmd = new FbCommand(
            "SELECT PP.RDB$PROCEDURE_NAME, PP.RDB$PARAMETER_NAME, PP.RDB$PARAMETER_TYPE, " +
            "PP.RDB$PARAMETER_NUMBER, PP.RDB$DEFAULT_SOURCE, " +
            "F.RDB$FIELD_TYPE, F.RDB$FIELD_SUB_TYPE, F.RDB$FIELD_LENGTH, F.RDB$FIELD_PRECISION, " +
            "F.RDB$FIELD_SCALE, F.RDB$CHARACTER_LENGTH " +
            "FROM RDB$PROCEDURE_PARAMETERS PP " +
            "JOIN RDB$FIELDS F ON F.RDB$FIELD_NAME = PP.RDB$FIELD_SOURCE " +
            "JOIN RDB$PROCEDURES P ON P.RDB$PROCEDURE_NAME = PP.RDB$PROCEDURE_NAME " +
            "WHERE P.RDB$SYSTEM_FLAG = 0 " +
            "ORDER BY PP.RDB$PROCEDURE_NAME, PP.RDB$PARAMETER_TYPE, PP.RDB$PARAMETER_NUMBER",
            connection);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var procedure = procedures[reader.GetTrimmedString(0)];

            var typeInfo = FirebirdTypeMapper.Map(
                Convert.ToInt16(reader.GetValue(5)),
                reader.GetNullableInt16(6),
                reader.GetNullableInt32(7),
                reader.GetNullableInt32(8),
                reader.GetNullableInt16(9),
                reader.GetNullableInt32(10));

            var parameter = new ProcedureParameter
            {
                Name = reader.GetTrimmedString(1),
                DataType = typeInfo.ToSqlText(),
                Position = Convert.ToInt32(reader.GetValue(3)) + 1,
                DefaultValue = StripEqualsPrefix(reader.GetNullableString(4))
            };

            var isOutput = Convert.ToInt16(reader.GetValue(2)) == 1;
            (isOutput ? procedure.OutputParameters : procedure.InputParameters).Add(parameter);
        }
    }

    private static string? StripEqualsPrefix(string? source)
    {
        if (source is null)
            return null;

        var trimmed = source.Trim();
        if (trimmed.StartsWith('='))
            trimmed = trimmed[1..].TrimStart();

        return trimmed;
    }
}
