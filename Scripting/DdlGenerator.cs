using System.Text;
using DbMetaTool.Firebird;
using DbMetaTool.Metadata;

namespace DbMetaTool.Scripting;

/// <summary>
/// Model -> DDL text generator (architecture.md: a standalone step, so nothing downstream
/// ever has to parse SQL back into the model). Each method returns one complete statement
/// with no trailing semicolon — a single FbCommand is one prepared DSQL statement, and the
/// semicolon is a script/isql statement-separator convention, not part of that grammar.
/// Clause order (type -> DEFAULT -> NOT NULL -> CHECK) verified empirically against a live
/// Firebird 5.0 instance before writing this, see the approved plan.
/// </summary>
public static class DdlGenerator
{
    public static string GenerateCreateDomain(DomainDefinition domain)
    {
        var typeText = new FirebirdTypeInfo(domain.BaseType, domain.Length, domain.Precision, domain.Scale).ToSqlText();

        var sb = new StringBuilder();
        sb.Append($"CREATE DOMAIN {domain.Name} AS {typeText}");

        if (domain.DefaultValue is not null)
            sb.Append($" DEFAULT {domain.DefaultValue}");
        if (!domain.Nullable)
            sb.Append(" NOT NULL");
        if (domain.CheckExpression is not null)
            sb.Append($" CHECK ({domain.CheckExpression})");

        return sb.ToString();
    }

    public static string GenerateCreateTable(TableDefinition table)
    {
        var columns = table.Columns.OrderBy(c => c.Position).Select(FormatColumn);
        return $"CREATE TABLE {table.Name} (\n  " + string.Join(",\n  ", columns) + "\n)";
    }

    /// <summary>ALTER TABLE ... ADD &lt;col_def&gt; — same column-clause syntax as CREATE TABLE, reuses FormatColumn.</summary>
    public static string GenerateAddColumn(string tableName, ColumnDefinition column) =>
        $"ALTER TABLE {tableName} ADD {FormatColumn(column)}";

    public static string GenerateCreateProcedure(ProcedureDefinition procedure) =>
        BuildProcedureDdl("CREATE PROCEDURE", procedure);

    /// <summary>Safe to run unconditionally, even on an unchanged procedure — Firebird idiom, no DROP, preserves grants.</summary>
    public static string GenerateCreateOrAlterProcedure(ProcedureDefinition procedure) =>
        BuildProcedureDdl("CREATE OR ALTER PROCEDURE", procedure);

    private static string BuildProcedureDdl(string header, ProcedureDefinition procedure)
    {
        var sb = new StringBuilder();
        sb.Append($"{header} {procedure.Name}");

        if (procedure.InputParameters.Count > 0)
        {
            var inputs = procedure.InputParameters.OrderBy(p => p.Position).Select(FormatParameter);
            sb.Append(" (\n  " + string.Join(",\n  ", inputs) + "\n)");
        }

        if (procedure.OutputParameters.Count > 0)
        {
            var outputs = procedure.OutputParameters.OrderBy(p => p.Position).Select(FormatParameter);
            sb.Append("\nRETURNS (\n  " + string.Join(",\n  ", outputs) + "\n)");
        }

        sb.Append("\nAS\n");
        sb.Append(procedure.SourceBody);

        return sb.ToString();
    }

    private static string FormatColumn(ColumnDefinition column)
    {
        var typeOrDomain = column.DomainName ?? column.InlineType;
        var text = $"{column.Name} {typeOrDomain}";

        if (column.DefaultValue is not null)
            text += $" DEFAULT {column.DefaultValue}";
        if (!column.Nullable)
            text += " NOT NULL";

        return text;
    }

    private static string FormatParameter(ProcedureParameter parameter)
    {
        var text = $"{parameter.Name} {parameter.DataType}";
        if (parameter.DefaultValue is not null)
            text += $" = {parameter.DefaultValue}";

        return text;
    }
}
