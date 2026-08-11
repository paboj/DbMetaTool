# Środowisko lokalne: Firebird 5.0 w Dockerze

## Obraz: `firebirdsql/firebird` (oficjalny)

Oficjalny obraz projektu Firebird to `firebirdsql/firebird` (repo `FirebirdSQL/firebird-docker`
na GitHubie, aktywnie utrzymywany). Używać tagu `5.0.4` albo `5`, nie `latest` — żeby wersja
nie zmieniała się pod nogami. Wspiera `linux/amd64` i `linux/arm64`.

## Host/port: `localhost:3050` jako stałe założenie

`BuildDatabase(string databaseDirectory, string scriptsDirectory)` dostaje sam katalog, nie
connection string — sygnatura nie przewiduje przekazania hosta/portu. Przy Firebird w trybie
client/server (kontener czy nie) `databaseDirectory` musi więc być interpretowany jako ścieżka
**po stronie serwera**, a host/port trzeba założyć w kodzie na sztywno (`localhost:3050`).
To założenie wystarcza dla scenariusza z zadania (jedna maszyna dev/test).

## Wolumen i ścieżki — nie mylić z ścieżką Windows

Obraz ma `VOLUME /var/lib/firebird/data` i `EXPOSE 3050/tcp`. Montować katalog hosta na
`/var/lib/firebird/data` (`-v ./data:/var/lib/firebird/data`), a jako `--db-dir` zawsze
podawać ścieżkę **kontenerową** (np. `/var/lib/firebird/data/moja-baza`), nigdy ścieżkę
Windows — to nie to samo miejsce na dysku, mimo że plik `.fdb` faktycznie ląduje też
w zamontowanym katalogu na hoście.

## IBExpert i sterownik .NET

- IBExpert wymaga lokalnie `fbclient.dll` do połączenia po TCP (domyślny `gds32.dll` z
  auth pluginem FB5 nie działa) — potwierdzone w poprzedniej sesji, plik już był w
  folderze instalacyjnym IBExpert, nic nie trzeba było pobierać.
- `FirebirdSql.Data.FirebirdClient` (**10.3.4**, dodany do projektu) w trybie
  remote/client-server jest czysto zarządzany — **potwierdzone testem 2026-08-11**
  (`scratchpad/FbConnTest/`): connection string bez `fbclient.dll`/`fbembed.dll` (sam
  `DataSource`/`Port`/`Database`) wystarczył do połączenia z kontenerem `fb-dbmetatool`
  i odczytu `manual-test.fdb`. Żadna natywna biblioteka nie jest potrzebna w kodzie
  `DbMetaTool` — to dotyczy tylko IBExpert jako osobnego narzędzia.

## Backup w scenariuszu testowym (krok 5 z `scope-and-acceptance.md`)

`gbak` jest wbudowany w obraz — backup/restore robić przez
`docker exec <container> gbak ...` zamiast instalować narzędzia dodatkowo na Windows.

## Dane logowania (BuildDatabase): localhost:3050 / SYSDBA/masterkey, stałe w kodzie

**Zaszyte na sztywno jako nazwane stałe w kodzie**
Sygnatura `BuildDatabase(databaseDirectory, scriptsDirectory)` nie przewiduje connection
stringa, na ten moment host/port/user/hasło hardcode default gdzieś w kodzie. Plik JSON z
fallbackiem na te same wartości nie dodaje realnej wartości: scenariusz zakłada jedną
maszynę dev/test (fallback byłby używany zawsze), a odbiorca kodu ma dostęp do źródeł.

- host:port — `localhost:3050`
- user/hasło — `SYSDBA` / `masterkey` (znane domyślne dane Firebirda, nie sekret —
  bezpieczne w publicznym repo)

Konsekwencja: `databaseDirectory` to ścieżka **po stronie serwera** (kontenerowa), nie ścieżka
Windows z przykładu w komentarzu `Program.cs` — do udokumentowania w README, sekcja "jak
uruchomić", z przykładem: `build-db --db-dir "/var/lib/firebird/data/mydb" --scripts-dir "..."`.
