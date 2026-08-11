# DbMetaTool — kontekst dla Claude Code

Aplikacja konsolowa .NET 8, generująca/aplikująca skrypty metadanych (domeny, tabele,
procedury) dla Firebird 5.0. Zadanie rekrutacyjne.

Szczegółowe reguły podzielone tematycznie w `.claude/rules/`:
- `architecture.md` — model metadanych, format `scripts-dir` (JSON = źródło prawdy), kolejność DDL
- `update-db-diff.md` — strategia diff przy `update-db` (najtrudniejsza część)
- `firebird.md` — specyfika Firebird 5.0 (RDB$ tabele, BLOB, driver)
- `scope-and-acceptance.md` — zakres zadania, scenariusz testowy, czego nie zakładać bez pytania
- `dev-environment.md` — Firebird 5.0 w Dockerze (obraz, port, wolumeny, IBExpert)

## IMPORTANT — twardy wymóg, nie zmieniać

`Program.cs` ma ustalone sygnatury: `BuildDatabase(string databaseDirectory, string scriptsDirectory)`,
`ExportScripts(string connectionString, string outputDirectory)`,
`UpdateDatabase(string connectionString, string scriptsDirectory)`, prywatną metodę
`GetArgValue` i strukturę `Main`. Wolno tylko uzupełnić ciała metod (TODO) i dodawać nowe
pliki/klasy. Nic więcej w `Program.cs` się nie zmienia.

## Konwencje pracy w repo

- `Program.cs` zostaje cienkim entry pointem; cała logika w osobnych plikach/klasach
  (np. katalogi `Metadata/`, `Firebird/`, `Scripting/`).
- Serwer Firebird 5.0 i IBExpert to narzędzia zewnętrzne (nie część repo) — używane do
  ręcznego przygotowania bazy testowej i weryfikacji scenariusza.
- Repo ma trafić na GitHub jako publiczne — pamiętać o README z krótkim opisem aplikacji,
  sposobem użycia (przykłady wywołań z trzema komendami) i świadomym opisem scenariuszy,
  które nie działają w pełni (jeśli takie zostaną).

## Status implementacji

- [x] `BuildDatabase`
- [x] `ExportScripts`
- [x] `UpdateDatabase`

(aktualizować w miarę postępu prac)
