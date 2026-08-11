using System;
using System.IO;
using DbMetaTool.Firebird;
using DbMetaTool.Metadata;
using DbMetaTool.Metadata.Json;
using DbMetaTool.Scripting;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool
{
    public static class Program
    {
        // Przykładowe wywołania:
        // DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
        // DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
        // DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Użycie:");
                Console.WriteLine("  build-db --db-dir <ścieżka> --scripts-dir <ścieżka>");
                Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <ścieżka>");
                Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <ścieżka>");
                return 1;
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "build-db":
                        {
                            string dbDir = GetArgValue(args, "--db-dir");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            BuildDatabase(dbDir, scriptsDir);
                            Console.WriteLine("Baza danych została zbudowana pomyślnie.");
                            return 0;
                        }

                    case "export-scripts":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string outputDir = GetArgValue(args, "--output-dir");

                            ExportScripts(connStr, outputDir);
                            Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
                            return 0;
                        }

                    case "update-db":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            UpdateDatabase(connStr, scriptsDir);
                            Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");
                            return 0;
                        }

                    default:
                        Console.WriteLine($"Nieznane polecenie: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd: " + ex.Message);
                return -1;
            }
        }

        private static string GetArgValue(string[] args, string name)
        {
            int idx = Array.IndexOf(args, name);
            if (idx == -1 || idx + 1 >= args.Length)
                throw new ArgumentException($"Brak wymaganego parametru {name}");
            return args[idx + 1];
        }

        /// <summary>
        /// Buduje nową bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
        {
            var connectionString = FirebirdConnectionFactory.BuildConnectionString(databaseDirectory);
            FbConnection.CreateDatabase(connectionString);

            using var connection = new FbConnection(connectionString);
            connection.Open();

            var model = ScriptsDirectoryStore.Load(scriptsDirectory);

            foreach (var domain in model.Domains)
                ExecuteDdl(connection, DdlGenerator.GenerateCreateDomain(domain), "domenę", domain.Name);

            foreach (var table in model.Tables)
                ExecuteDdl(connection, DdlGenerator.GenerateCreateTable(table), "tabelę", table.Name);

            foreach (var procedure in model.Procedures)
                ExecuteDdl(connection, DdlGenerator.GenerateCreateProcedure(procedure), "procedurę", procedure.Name);
        }

        private static void ExecuteDdl(FbConnection connection, string ddl, string objectType, string objectName)
        {
            try
            {
                using var cmd = new FbCommand(ddl, connection);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Błąd tworzenia obiektu (typ: {objectType}, nazwa: {objectName}): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generuje skrypty metadanych z istniejącej bazy danych Firebird 5.0.
        /// </summary>
        public static void ExportScripts(string connectionString, string outputDirectory)
        {
            using var connection = new FbConnection(connectionString);
            connection.Open();

            var model = new MetadataModel
            {
                Domains = DomainReader.ReadAll(connection),
                Tables = TableReader.ReadAll(connection),
                Procedures = ProcedureReader.ReadAll(connection)
            };

            ScriptsDirectoryStore.Save(model, outputDirectory);
        }

        /// <summary>
        /// Aktualizuje istniejącą bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void UpdateDatabase(string connectionString, string scriptsDirectory)
        {
            var sourceModel = ScriptsDirectoryStore.Load(scriptsDirectory);

            using var connection = new FbConnection(connectionString);
            connection.Open();

            var targetDomains = DomainReader.ReadAll(connection).ToDictionary(d => d.Name, StringComparer.Ordinal);
            var targetTables = TableReader.ReadAll(connection).ToDictionary(t => t.Name, StringComparer.Ordinal);

            var warnings = new List<string>();
            UpdateDomains(connection, sourceModel.Domains, targetDomains, warnings);
            UpdateTables(connection, sourceModel.Tables, targetTables, warnings);
            UpdateProcedures(connection, sourceModel.Procedures, warnings);

            ReportWarnings(warnings);
        }

        private static void UpdateDomains(FbConnection connection, List<DomainDefinition> sourceDomains,
            Dictionary<string, DomainDefinition> targetDomains, List<string> warnings)
        {
            foreach (var domain in sourceDomains)
            {
                if (!targetDomains.TryGetValue(domain.Name, out var existing))
                    TryExecuteDdl(connection, DdlGenerator.GenerateCreateDomain(domain), "domenę", domain.Name, warnings);
                else if (!DomainsEqual(domain, existing))
                    warnings.Add($"Domena {domain.Name}: różni się od wersji w bazie — pominięto (ALTER DOMAIN nieobsługiwany).");
            }
        }

        private static void UpdateTables(FbConnection connection, List<TableDefinition> sourceTables,
            Dictionary<string, TableDefinition> targetTables, List<string> warnings)
        {
            foreach (var table in sourceTables)
            {
                if (!targetTables.TryGetValue(table.Name, out var existing))
                {
                    TryExecuteDdl(connection, DdlGenerator.GenerateCreateTable(table), "tabelę", table.Name, warnings);
                    continue;
                }

                var targetColumns = existing.Columns.ToDictionary(c => c.Name, StringComparer.Ordinal);
                foreach (var column in table.Columns)
                {
                    if (!targetColumns.TryGetValue(column.Name, out var existingColumn))
                        TryExecuteDdl(connection, DdlGenerator.GenerateAddColumn(table.Name, column),
                            "kolumnę", $"{table.Name}.{column.Name}", warnings);
                    else if (!ColumnsEqual(column, existingColumn))
                        warnings.Add($"Kolumna {table.Name}.{column.Name}: różni się od wersji w bazie — pominięto (ALTER COLUMN nieobsługiwany).");
                }
            }
        }

        private static void UpdateProcedures(FbConnection connection, List<ProcedureDefinition> sourceProcedures, List<string> warnings)
        {
            foreach (var procedure in sourceProcedures)
                TryExecuteDdl(connection, DdlGenerator.GenerateCreateOrAlterProcedure(procedure), "procedurę", procedure.Name, warnings);
        }

        private static void TryExecuteDdl(FbConnection connection, string ddl, string objectType, string objectName, List<string> warnings)
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                using var cmd = new FbCommand(ddl, connection, transaction);
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                warnings.Add($"Błąd aktualizacji obiektu (typ: {objectType}, nazwa: {objectName}): {ex.Message}");
            }
        }

        private static void ReportWarnings(List<string> warnings)
        {
            if (warnings.Count == 0)
                return;

            Console.WriteLine($"Aktualizacja zakończona z ostrzeżeniami ({warnings.Count}):");
            foreach (var warning in warnings)
                Console.WriteLine("  - " + warning);
        }

        private static bool DomainsEqual(DomainDefinition a, DomainDefinition b) =>
            string.Equals(a.BaseType, b.BaseType, StringComparison.Ordinal)
            && a.Length == b.Length && a.Precision == b.Precision && a.Scale == b.Scale
            && a.Nullable == b.Nullable
            && string.Equals(a.DefaultValue, b.DefaultValue, StringComparison.Ordinal)
            && string.Equals(a.CheckExpression, b.CheckExpression, StringComparison.Ordinal);

        private static bool ColumnsEqual(ColumnDefinition a, ColumnDefinition b) =>
            string.Equals(a.DomainName, b.DomainName, StringComparison.Ordinal)
            && string.Equals(a.InlineType, b.InlineType, StringComparison.Ordinal)
            && a.Nullable == b.Nullable
            && string.Equals(a.DefaultValue, b.DefaultValue, StringComparison.Ordinal);
    }
}
