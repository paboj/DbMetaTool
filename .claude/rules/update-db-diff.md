# update-db: strategia diff (najtrudniejsza część zadania)

Dla każdego obiektu w źródle (JSON) porównaj z odpowiednikiem w bazie docelowej
(odczytanym z RDB$ system tables):

- brak w targecie → CREATE
- różni się →
  - procedury: `CREATE OR ALTER PROCEDURE` (idiom FB, zachowuje uprawnienia, bez DROP)
  - tabele: `ALTER TABLE ADD COLUMN` dla nowych kolumn; jeśli kolumna istnieje w obu, ale
    różni się typem/rozmiarem — pomiń ją z ostrzeżeniem w raporcie, nie przerywaj update-db
    (decyzja i uzasadnienie: `scope-and-acceptance.md`)
- identyczny → pomiń (idempotentność — scenariusz wymaga braku błędów na niezmienionych
  obiektach)

Nie usuwać obiektów nieobecnych już w źródle — poza zakresem zadania.

Każda zmiana tej strategii musi przejść scenariusz z `scope-and-acceptance.md`, punkt 4.
