# update-db: strategia diff (najtrudniejsza część zadania)

Dla każdego obiektu w źródle (JSON) porównaj z odpowiednikiem w bazie docelowej
(odczytanym z RDB$ system tables — reużyć `Firebird/DomainReader.cs`/`TableReader.cs`/
`ProcedureReader.cs` z `ExportScripts`, nie pisać drugiego czytnika):

- brak w targecie → CREATE
- różni się →
  - domeny: pomiń z ostrzeżeniem w raporcie, nie implementować `ALTER DOMAIN` — te same
    ograniczenia FB na zmianę typu co przy kolumnach (decyzja i uzasadnienie:
    `scope-and-acceptance.md`)
  - procedury: zawsze `CREATE OR ALTER PROCEDURE`, bez porównywania treści — idiom FB jest
    bezpieczny do wykonania nawet na niezmienionej procedurze (no-op), więc diff tu jest
    zbędny, wystarczy zawsze emitować
  - tabele: `ALTER TABLE ADD COLUMN` dla nowych kolumn; jeśli kolumna istnieje w obu, ale
    różni się typem/rozmiarem — pomiń ją z ostrzeżeniem w raporcie, nie przerywaj update-db
    (decyzja i uzasadnienie: `scope-and-acceptance.md`)
- identyczny → pomiń (idempotentność — scenariusz wymaga braku błędów na niezmienionych
  obiektach; procedury spełniają to przez samo `CREATE OR ALTER`, nie przez wykrywanie
  identyczności)

Nie usuwać obiektów nieobecnych już w źródle — poza zakresem zadania.

Każda zmiana tej strategii musi przejść scenariusz z `scope-and-acceptance.md`, punkt 4.
