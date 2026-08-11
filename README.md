# DbMetaTool

Aplikacja konsolowa (.NET 8) do generowania i aplikowania skryptów metadanych
(domeny, tabele, procedury) dla bazy Firebird 5.0.

Trzy komendy:
- `export-scripts` — eksportuje metadane z istniejącej bazy do plików JSON.
- `build-db` — buduje nową, pustą bazę na podstawie tych skryptów.
- `update-db` — aktualizuje istniejącą bazę różnicowo (bez drop & recreate).

## Wymagania

- .NET 8 SDK
- Serwer Firebird 5.0 dostępny pod `localhost:3050`, user `SYSDBA`, hasło `masterkey`
  (domyślne dane Firebirda, zaszyte na sztywno w `build-db` — patrz sekcja niżej)

Najprościej: Firebird 5.0 w Dockerze, obraz oficjalny `firebirdsql/firebird:5.0.4`.
Uruchomienie kontenera (zgodne z dokumentacją obrazu — port 3050 wystawiony na hosta,
katalog danych zamontowany, hasło `SYSDBA` ustawione na `masterkey`):

```bash
docker run -d --name fb-dbmetatool \
  -p 3050:3050 \
  -e FIREBIRD_ROOT_PASSWORD=masterkey \
  -v ./data:/var/lib/firebird/data \
  firebirdsql/firebird:5.0.4
```

## Użycie

### export-scripts

Eksportuje domeny, tabele (z kolumnami) i procedury (z parametrami i treścią) z istniejącej
bazy do katalogu jako pliki JSON — jeden plik na obiekt, pod `domains/`, `tables/`,
`procedures/`.

```
dotnet run -- export-scripts \
  --connection-string "User=SYSDBA;Password=masterkey;DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/mydb.fdb;Dialect=3;Charset=UTF8;" \
  --output-dir "C:\sciezka\do\wyjscia"
```

### build-db

Tworzy nową, pustą bazę Firebird pod wskazaną ścieżką i wykonuje w niej skrypty z
`--scripts-dir`, w kolejności domeny → tabele → procedury.

```
dotnet run -- build-db --db-dir "/var/lib/firebird/data/nowabaza.fdb" --scripts-dir "C:\sciezka\do\skryptow"
```

**Uwaga:** `--db-dir` to ścieżka **po stronie serwera Firebirda** (np. wewnątrz kontenera
Docker), nie ścieżka na maszynie, z której uruchamiasz `DbMetaTool` — to nie to samo
miejsce na dysku, nawet jeśli katalog danych jest zamontowany też na hoście. Host, port i
dane logowania (`localhost:3050`, `SYSDBA`/`masterkey`) są zaszyte na sztywno w kodzie,
bo sygnatura `BuildDatabase(databaseDirectory, scriptsDirectory)` nie przewiduje
connection stringa.

### update-db

Aktualizuje istniejącą bazę na podstawie skryptów — różnicowo, nie przez drop & recreate.

```
dotnet run -- update-db \
  --connection-string "User=SYSDBA;Password=masterkey;DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/mydb.fdb;Dialect=3;Charset=UTF8;" \
  --scripts-dir "C:\sciezka\do\skryptow"
```

## Format scripts-dir

Metadane są przechowywane jako JSON, nie jako surowy SQL — to źródło prawdy, z którego
generowany jest DDL (osobny generator, model → SQL). Jeden plik na obiekt:

```
scripts-dir/
  domains/
    D_PRODUCT_NAME.json
  tables/
    STOCK_ITEMS.json
  procedures/
    GET_FRIDGE_ITEMS.json
```

## Zakres

Obsługiwane: domeny, tabele (z kolumnami), procedury.
Pomijane: constraints, triggery, indeksy i inne obiekty bazy.

## Znane ograniczenia

- **`update-db` nie usuwa obiektów.** Jeśli obiekt zniknął ze źródła, zostaje w bazie
  docelowej nietknięty — aktualizacja jest wyłącznie addytywna (`CREATE`,
  `ALTER TABLE ADD COLUMN`, `CREATE OR ALTER PROCEDURE`). Jednokierunkowe kasowanie
  "czego nie ma w źródle" uznano za zbyt ryzykowne: niepełny albo błędny eksport mógłby
  skasować obiekty celowo zostawione w bazie.
- **`update-db` nie zmienia typu/rozmiaru istniejącej kolumny ani definicji istniejącej
  domeny** (brak `ALTER COLUMN`/`ALTER DOMAIN`). Firebird ma nietrywialne ograniczenia na
  tego typu zmiany (nie każda jest bezpieczna bez utraty danych) — pełna obsługa
  wykraczałaby poza uproszczony zakres tego zadania. Zamiast twardego błędu całej
  aktualizacji: niezgodny obiekt jest pomijany z ostrzeżeniem w konsoli, reszta
  aktualizacji przebiega dalej.

## Architektura (skrót)

- `Metadata/` — model metadanych (POCO) + serializacja do/z `scripts-dir` (JSON).
- `Firebird/` — odczyt `RDB$...` system tables → model (reużywany przez `export-scripts`
  i `update-db`), connection string dla `build-db`.
- `Scripting/` — generator DDL: model → tekst SQL (`build-db`, `update-db`).
- `Program.cs` — cienki entry point, cała logika w powyższych katalogach.
