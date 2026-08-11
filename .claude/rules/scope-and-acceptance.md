# Zakres i kryterium akceptacji

## Zakres (uproszczony, zgodnie z treścią zadania)

Obsługiwane: domeny, tabele (z kolumnami), procedury.
Pomijamy: constraints, triggery, indeksy i inne obiekty.
`update-db` **nie usuwa** obiektów — task tego nie wymaga wprost.

## Scenariusz testowy — kryterium akceptacji dla każdej decyzji

1. Ręczna baza (kilka domen/tabel/procedur) → `export-scripts`.
2. `build-db` w innym katalogu ze skryptów → obie bazy identyczne strukturalnie.
3. Backup bazy → zmiany: nowa procedura, zmodyfikowana procedura (+parametr), tabela
   +nowa kolumna → `export-scripts` ponownie → zmiany widoczne w wyniku.
4. `update-db` na backupie (starsza wersja) nowymi skryptami → aktualizacja **różnicowa**,
   bez błędów na niezmienionych obiektach.

Jeśli propozycja architektury nie przechodzi tego scenariusza (zwłaszcza pkt 4), jest zła.

## Decyzje zakresu (rozstrzygnięte)

**Usuwanie obiektów przy `update-db`: nie implementować.**
Dlaczego: task tego nie wymaga, scenariusz tego nie testuje, a jednokierunkowe kasowanie
"czego nie ma w źródle" jest ryzykowne — niepełny albo błędny eksport mógłby skasować
obiekty, które ktoś celowo zostawił w bazie. Bezpieczniejszy jest addytywny update.
Udokumentować w README jako świadomie niezaimplementowany scenariusz (zadanie wprost tego
wymaga w sekcji "Sposób dostarczenia").

**Zmiana typu/rozmiaru istniejącej kolumny: nie implementować ALTER, wykryć różnicę i
pominąć tę kolumnę z ostrzeżeniem w raporcie, kontynuując update pozostałych obiektów.**
Dlaczego: nie ma tego w scenariuszu testowym (pkt 3 to tylko dodanie kolumny, nie zmiana
istniejącej). Firebird ma nietrywialne ograniczenia na `ALTER COLUMN` (nie każda zmiana
typu/rozmiaru jest bezpieczna bez utraty danych) — pełna obsługa wykracza poza
"uproszczony" zakres zadania. Twardy fail całego `update-db` z powodu jednej niezgodnej
kolumny byłby gorszy niż częściowa, jawnie zaraportowana aktualizacja — pkt 4 scenariusza
wymaga braku błędów na obiektach, które da się bezpiecznie zaktualizować. Udokumentować
w README jako ograniczenie.

**Zmiana istniejącej domeny: nie implementować `ALTER DOMAIN`, wykryć różnicę i pominąć
z ostrzeżeniem w raporcie, kontynuując update pozostałych obiektów.**
Dlaczego: ta sama logika co przy zmianie kolumny — `ALTER DOMAIN` ma nietrywialne
ograniczenia na zmianę typu w Firebirdzie, a scenariusz testowy i tak nigdy nie testuje
zmienionej domeny (pkt 3 zmienia tylko procedurę i dodaje kolumnę). Spójność z już podjętą
decyzją dla kolumn ważniejsza niż implementowanie nieprzetestowanej ścieżki.

## Do zweryfikowania (fakt, nie decyzja)

`RDB$PROCEDURE_SOURCE` i `RDB$PROCEDURE_PARAMETERS` (typ/pozycja/IN-OUT/default) —
zweryfikowane 2026-08-11 testem przez driver, patrz `firebird.md`. Kluczowy wniosek do
pamiętania przy pisaniu `ExportScripts`: `RDB$FIELD_SOURCE` w `RDB$PROCEDURE_PARAMETERS`
zawsze wskazuje na (ew. systemową) domenę — typ parametru wymaga joina do `RDB$FIELDS`,
nie da się go odczytać wprost z `RDB$PROCEDURE_PARAMETERS`.
