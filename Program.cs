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
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Wykonaj skrypty z katalogu scriptsDirectory (tylko obsługiwane elementy).
            // 3) Zadbaj o poprawną kolejność i bezpieczeństwo zmian.
            throw new NotImplementedException();
        }
    }
}
