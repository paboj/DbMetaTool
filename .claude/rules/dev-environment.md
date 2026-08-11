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

## IBExpert i sterownik .NET — do zweryfikowania

- IBExpert łączy się przez sieć (`localhost/3050:/var/lib/firebird/data/plik.fdb`) — do
  sprawdzenia, czy do samego połączenia po TCP wymaga lokalnie `fbclient.dll`, czy
  wystarczy sam adres serwera (szybki test: spróbować połączyć się bez instalowania FB
  lokalnie na Windows).
- `FirebirdSql.Data.FirebirdClient` w trybie remote/client-server powinien być czysto
  zarządzanym providerem (bez natywnych bibliotek) — natywne biblioteki (`fbembed`) są
  potrzebne tylko w trybie embedded, którego tu nie używamy. Potwierdzić szybkim testem
  połączenia z hosta do kontenera, zanim się na tym oprze reszta implementacji.

## Backup w scenariuszu testowym (krok 5 z `scope-and-acceptance.md`)

`gbak` jest wbudowany w obraz — backup/restore robić przez
`docker exec <container> gbak ...` zamiast instalować narzędzia dodatkowo na Windows.
