# DbMetaTool

Aplikacja konsolowa (.NET 8) do generowania i aplikowania skryptów metadanych
(domeny, tabele, procedury) dla bazy Firebird 5.0.

Trzy komendy:
- `export-scripts` — eksportuje metadane z istniejącej bazy do plików JSON.
- `build-db` — buduje nową, pustą bazę na podstawie tych skryptów.
- `update-db` — aktualizuje istniejącą bazę różnicowo (bez drop & recreate).

## Wymagania

- .NET 8 SDK
- Serwer Firebird 5.0 pod `localhost:3050`, user `SYSDBA`, hasło `masterkey` — patrz
  "Baza testowa" niżej.

## Baza testowa — pierwsze uruchomienie

Kontener + baza `manual-test.fdb` z przykładowymi danymi:

```bash
docker run -d --name fb-dbmetatool \
  -p 3050:3050 \
  -e FIREBIRD_ROOT_PASSWORD=masterkey \
  -e FIREBIRD_DATABASE=manual-test.fdb \
  -e FIREBIRD_DATABASE_DEFAULT_CHARSET=UTF8 \
  -v ./data:/var/lib/firebird/data \
  -v ./docker/initdb:/docker-entrypoint-initdb.d:ro \
  firebirdsql/firebird:5.0.4
```

Skrypty init:
[`01-domains.sql`](docker/initdb/01-domains.sql),
[`02-tables.sql`](docker/initdb/02-tables.sql),
[`03-procedures.sql`](docker/initdb/03-procedures.sql) — uruchamiane tylko przy pustym
`./data`.

## Użycie

### export-scripts

Eksportuje domeny, tabele (z kolumnami) i procedury (z parametrami i treścią) z istniejącej
bazy do katalogu jako pliki JSON — jeden plik na obiekt, pod `domains/`, `tables/`,
`procedures/`.

```bash
dotnet run -- export-scripts \
  --connection-string "User=SYSDBA;Password=masterkey;DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/manual-test.fdb;Dialect=3;Charset=UTF8;" \
  --output-dir "<sciezka-do-wyjscia>"
```

### build-db

Tworzy nową, pustą bazę Firebird pod wskazaną ścieżką i wykonuje w niej skrypty z
`--scripts-dir`, w kolejności domeny → tabele → procedury.

```bash
dotnet run -- build-db \
  --db-dir "/var/lib/firebird/data/<nazwa-nowej-bazy>.fdb" \
  --scripts-dir "<sciezka-do-skryptow>"
```

**Uwaga:** `--db-dir` to ścieżka **po stronie serwera Firebirda** (np. wewnątrz kontenera
Docker), nie ścieżka na maszynie, z której uruchamiasz `DbMetaTool` — to nie to samo
miejsce na dysku, nawet jeśli katalog danych jest zamontowany też na hoście. Host, port i
dane logowania (`localhost:3050`, `SYSDBA`/`masterkey`) są zaszyte na sztywno w kodzie,
bo sygnatura `BuildDatabase(databaseDirectory, scriptsDirectory)` nie przewiduje
connection stringa — pełne uzasadnienie wyboru zdalnej ścieżki zamiast lokalnego pliku
`.fdb`: patrz "Decyzja projektowa" na końcu README.

### Przygotowanie testu update-db: backup i zmiany na bazie

Żeby przetestować `update-db` na starszej wersji bazy, najpierw robimy backup bieżącego
stanu, potem wprowadzamy zmiany:

```bash
docker exec fb-dbmetatool gbak -b -user SYSDBA -pas masterkey \
  localhost:/var/lib/firebird/data/manual-test.fdb \
  /var/lib/firebird/data/manual-test-baseline.fbk

docker exec -i fb-dbmetatool isql -user SYSDBA -password masterkey \
  /var/lib/firebird/data/manual-test.fdb < docker/manual-changes/round-2-changes.sql
```

[`round-2-changes.sql`](docker/manual-changes/round-2-changes.sql) dodaje kolumnę do
istniejącej tabeli, modyfikuje istniejącą procedurę (dodatkowy parametr) i dodaje nową
procedurę.

Uruchomienie `export-scripts` ponownie na `manual-test.fdb` pokaże te zmiany w wyniku.
Żeby przetestować `update-db`, przywróć backup jako osobną bazę (starszą wersję) i na niej
uruchom `update-db` z nowym `scripts-dir`:

```bash
docker exec fb-dbmetatool gbak -c -user SYSDBA -pas masterkey \
  /var/lib/firebird/data/manual-test-baseline.fbk \
  localhost:/var/lib/firebird/data/manual-test-baseline.fdb
```

### update-db

Aktualizuje istniejącą bazę na podstawie skryptów — różnicowo, nie przez drop & recreate.

```bash
dotnet run -- update-db \
  --connection-string "User=SYSDBA;Password=masterkey;DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/manual-test-baseline.fdb;Dialect=3;Charset=UTF8;" \
  --scripts-dir "<sciezka-do-skryptow>"
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

- **Brak testów automatycznych.** Weryfikacja opiera się na ręcznym scenariuszu przez
  Docker (opisanym wyżej), nie na testach jednostkowych/integracyjnych — byłyby
  następnym krokiem w realizacji zadania.
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
- **`build-db` nie ma rollbacku przy błędzie w trakcie budowy.** W przeciwieństwie do
  `update-db` (każdy DDL w osobnej transakcji z rollbackiem przy błędzie, reszta
  aktualizacji kontynuowana), `build-db` przerywa całość na pierwszym błędzie — obiekty
  utworzone przed nim zostają w bazie. Świadomie zaakceptowane dla tej komendy: baza jest
  świeżo tworzona w tym samym wywołaniu, więc naprawa to usunięcie pliku `.fdb` i
  ponowne uruchomienie, a nie ręczne czyszczenie stanu.
- **Mapowanie typów obejmuje podzbiór typów Firebird**, wystarczający do scenariusza
  testowego: `SMALLINT`/`INTEGER`/`BIGINT` (z `NUMERIC`/`DECIMAL` przez subtype),
  `FLOAT`/`DOUBLE PRECISION`, `DATE`/`TIME`/`TIMESTAMP`, `CHAR`/`VARCHAR`, `BOOLEAN`,
  `BLOB SUB_TYPE TEXT`/inne bloby. Typy spoza tej listy (np. `INT128`, `DECFLOAT` z
  Firebird 4+) powodują `NotSupportedException` przy `export-scripts` zamiast cichego
  pominięcia — świadomy wybór "fail loud" zamiast eksportu z lukami.
- **Hardcodowane `SYSDBA`/`masterkey` + `docker run -p 3050:3050` (sekcja "Baza
  testowa") = port 3050 wystawiony na wszystkich interfejsach hosta, nie tylko
  `localhost`.** `-p` bez adresu wiąże się domyślnie z `0.0.0.0`. Bezpieczne w praktyce
  tylko dopóki hosta chroni firewall/NAT poza siecią lokalną — nie dzięki samej
  konfiguracji w tym repo. Ograniczenie do samego hosta: `-p 127.0.0.1:3050:3050`.
  Konfigurowalne poświadczenia (np. przez zmienne środowiskowe) dla serwera faktycznie
  dostępnego zdalnie — poza zakresem tego rozwiązania.

## Architektura (skrót)

- `Metadata/` — model metadanych (POCO) + serializacja do/z `scripts-dir` (JSON).
- `Firebird/` — odczyt `RDB$...` system tables → model (reużywany przez `export-scripts`
  i `update-db`), connection string dla `build-db`.
- `Scripting/` — generator DDL: model → tekst SQL (`build-db`, `update-db`).
- `Program.cs` — cienki entry point, cała logika w powyższych katalogach.

## Decyzja projektowa: `--db-dir` jako ścieżka zdalna, nie lokalna

`build-db` przyjmuje `databaseDirectory` (parametr `--db-dir`), nie connection string —
w przeciwieństwie do `export-scripts`/`update-db`, które wyraźnie oczekują connection
stringa i tym samym zakładają połączenie z serwerem Firebirda. Do rozstrzygnięcia było,
czy `--db-dir` powinno oznaczać ścieżkę **lokalną** (plik `.fdb` otwierany bezpośrednio,
bez serwera) czy **zdalną** (ścieżka po stronie serwera, tak jak dziś w tym README).

Ścieżka lokalna z pozoru lepiej pasowała do samej nazwy parametru. Nie pozwoliłaby jednak
uniknąć hardcodowania poświadczeń: Firebird embedded (`ServerType=1`) działa in-process,
bez nasłuchu sieciowego, ale nadal autoryzuje przez bazę bezpieczeństwa
(`security5.fdb`), więc `User`/`Password` (`SYSDBA`/`masterkey`) i tak trzeba by zaszyć
na stałe. Wymagałaby za to dostarczenia natywnej `fbclient.dll`
([FirebirdSQL/firebird](https://github.com/FirebirdSQL/firebird)) obok aplikacji, bo ten
tryb sterownika `FirebirdSql.Data.FirebirdClient` (w przeciwieństwie do trybu
remote/client-server, patrz `.claude/rules/dev-environment.md`) nie jest czysto
zarządzany — dodatkowa zależność zewnętrzna tylko dla jednej z trzech komend, bez
uniknięcia hardcodowania w zamian.

Oba warianty wymagają hardcodowanych poświadczeń, ale nie tego samego ryzyka. Lokalnie
hasło nie chroni niczego ponad system plików — kto uruchomi proces, ten i tak ma dostęp
do `.fdb`. Zdalnie po TCP hasło jest faktyczną granicą bezpieczeństwa — a przy
konfiguracji z sekcji "Baza testowa" ta granica jest słabsza, niż mogłoby się wydawać
(szczegóły: "Znane ograniczenia" wyżej, ostatni punkt).

Ważniejszy argument: **spójność z resztą programu.** `export-scripts` i `update-db`
jednoznacznie łączą się z serwerem bazy danych — `build-db` powinno robić to samo, a nie
działać na zupełnie innej zasadzie tylko dlatego, że jego sygnatura nie ma miejsca na
connection string. Naturalnym pierwszym odruchem byłoby ujednolicenie parametrów (np.
connection string ze ścieżką do jeszcze nieistniejącej bazy zamiast `--db-dir`) — ale
sygnatura `BuildDatabase(databaseDirectory, scriptsDirectory)` jest zadanym wymogiem
("IMPORTANT" w `CLAUDE.md`) i nie podlega zmianie. A skoro `FbConnection.CreateDatabase`
i tak wewnętrznie buduje connection string, to przy parametrze `--db-dir` reszta tego
stringa (host, port, użytkownik, hasło) musi gdzieś zostać ustalona na stałe — różnica
między podejściem lokalnym a zdalnym to tylko **które** pola są hardcodowane:

```
# lokalny (wymaga fbclient.dll obok aplikacji)
UserID=SYSDBA;Password=masterkey;Database=C:\Path\To\manual-test.fdb;ServerType=1;ClientLibrary=fbclient.dll

# zdalny (wybrany — spójny z export-scripts/update-db, czysto zarządzany sterownik)
User=SYSDBA;Password=masterkey;DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/manual-test.fdb;Dialect=3;Charset=UTF8;
```

Świadomie wybrany wariant zdalny: brak dodatkowej zależności natywnej, spójność
architektoniczna z pozostałymi dwiema komendami i mniejsze ryzyko rozjazdu zachowania
między nimi.
