namespace DbMetaTool.Firebird;

/// <summary>
/// Decomposed SQL type: a bare keyword plus optional length or precision/scale, mirroring
/// how RDB$FIELDS actually stores type info (separate columns, not one combined string).
/// </summary>
public readonly record struct FirebirdTypeInfo(string Keyword, int? Length, int? Precision, int? Scale)
{
    /// <summary>Composes a single SQL type string, e.g. "VARCHAR(100)", "NUMERIC(10, 2)", "INTEGER".</summary>
    public string ToSqlText()
    {
        if (Precision.HasValue)
            return $"{Keyword}({Precision}, {Scale ?? 0})";
        if (Length.HasValue)
            return $"{Keyword}({Length})";
        return Keyword;
    }
}

/// <summary>
/// Maps RDB$FIELD_TYPE/RDB$FIELD_SUB_TYPE (+ length/precision/scale) from RDB$FIELDS to a
/// SQL type. Single source of truth for this mapping — used both for
/// DomainDefinition.BaseType (decomposed fields, direct from RDB$FIELDS) and for
/// ColumnDefinition.InlineType / ProcedureParameter.DataType (composed via ToSqlText(),
/// after joining RDB$FIELD_SOURCE to RDB$FIELDS).
/// </summary>
public static class FirebirdTypeMapper
{
    public static FirebirdTypeInfo Map(short fieldType, short? subType, int? fieldLength,
        int? precision, short? scale, int? characterLength)
    {
        return fieldType switch
        {
            7 => MapIntegerFamily("SMALLINT", subType, precision, scale),
            8 => MapIntegerFamily("INTEGER", subType, precision, scale),
            16 => MapIntegerFamily("BIGINT", subType, precision, scale),
            10 => new FirebirdTypeInfo("FLOAT", null, null, null),
            27 => new FirebirdTypeInfo("DOUBLE PRECISION", null, null, null),
            12 => new FirebirdTypeInfo("DATE", null, null, null),
            13 => new FirebirdTypeInfo("TIME", null, null, null),
            35 => new FirebirdTypeInfo("TIMESTAMP", null, null, null),
            14 => new FirebirdTypeInfo("CHAR", characterLength, null, null),
            37 => new FirebirdTypeInfo("VARCHAR", characterLength, null, null),
            23 => new FirebirdTypeInfo("BOOLEAN", null, null, null),
            261 => new FirebirdTypeInfo(
                subType == 1 ? "BLOB SUB_TYPE TEXT" : $"BLOB SUB_TYPE {subType ?? 0}",
                null, null, null),
            _ => throw new NotSupportedException(
                $"Unsupported RDB$FIELD_TYPE {fieldType} (sub_type {(subType.HasValue ? subType.Value.ToString() : "null")}).")
        };
    }

    private static FirebirdTypeInfo MapIntegerFamily(string plainKeyword, short? subType, int? precision, short? scale)
    {
        // RDB$FIELD_SCALE is stored negative (decimal places); normalize to a positive count here.
        return subType switch
        {
            1 => new FirebirdTypeInfo("NUMERIC", null, precision, -(scale ?? 0)),
            2 => new FirebirdTypeInfo("DECIMAL", null, precision, -(scale ?? 0)),
            _ => new FirebirdTypeInfo(plainKeyword, null, null, null)
        };
    }
}
