# Architektura: model metadanych i DDL

## Źródło prawdy: JSON, nie surowy SQL

`scripts-dir` przechowuje model metadanych jako JSON (nie `.sql`). `update-db` musi
diffować obiekty ze skryptów względem stanu bazy (RDB$ system tables) — gdybyśmy
eksportowali `.sql`, `update-db` musiałby parsować SQL z powrotem do modelu obiektowego:
duplikacja logiki parsera i ryzyko rozjazdu z tym, co faktycznie wygenerował
`export-scripts`. JSON jako model + osobny generator DDL (model → SQL) eliminuje
parsowanie SQL w ogóle.

`.sql` jako dodatkowy, pomocniczy output do wglądu jest OK, ale nie jako źródło prawdy.

## Model metadanych (szkielet, nie kod)

- `DomainDefinition`: nazwa, typ bazowy, długość/precyzja/skala, nullable, default, check
- `TableDefinition`: nazwa, kolumny (nazwa, domena-lub-typ, nullable, default, pozycja)
- `ProcedureDefinition`: nazwa, parametry wejściowe, parametry wyjściowe, treść źródłowa

## Kolejność DDL: domeny → tabele → procedury

Tabele mogą referencjonować domeny, procedury mogą referencjonować tabele/domeny.
Ta sama kolejność obowiązuje przy `update-db`, per typ obiektu.

Diff przy `update-db` — patrz `update-db-diff.md`.
