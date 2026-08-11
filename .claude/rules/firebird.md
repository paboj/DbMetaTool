# Firebird 5.0 — specyfika do pilnowania

- Filtrować obiekty systemowe: `RDB$RELATIONS.RDB$SYSTEM_FLAG = 1` (analogicznie
  `RDB$PROCEDURES` itp.) — inaczej eksport zaśmieci się obiektami systemowymi.
- `CREATE OR ALTER PROCEDURE` — bezpieczna aktualizacja bez DROP, zachowuje granty.
- `RDB$PROCEDURE_SOURCE` i inne kolumny źródłowe to `BLOB SUB_TYPE TEXT` — czytać jako
  blob, nie zwykły string. Dokładny format niepewny bez sprawdzenia — zweryfikować
  zapytaniem w IBExpert zamiast zakładać.
- Nazwy w tabelach `RDB$*` są typu `CHAR` — mogą mieć trailing spaces, zawsze `.Trim()`.
- Sterownik: NuGet `FirebirdSql.Data.FirebirdClient` — potwierdzić wersję kompatybilną
  z FB5 + .NET 8 przed użyciem.
- Różnice względem SQL Server/Postgres nie są oczywiste — nie zakładać zachowania na
  podstawie doświadczenia z innych baz, weryfikować w IBExpert.

## Zapytania weryfikacyjne (do odpalenia w IBExpert, gdy wstanie Firebird w Dockerze)

```sql
-- Format i podtyp źródła procedury
SELECT RDB$PROCEDURE_NAME, RDB$PROCEDURE_SOURCE
FROM RDB$PROCEDURES
WHERE RDB$SYSTEM_FLAG = 0;

-- Parametry procedur: IN/OUT (0=input,1=output), typ, pozycja
SELECT RDB$PROCEDURE_NAME, RDB$PARAMETER_NAME, RDB$PARAMETER_TYPE,
       RDB$PARAMETER_NUMBER, RDB$FIELD_SOURCE
FROM RDB$PROCEDURE_PARAMETERS
ORDER BY RDB$PROCEDURE_NAME, RDB$PARAMETER_TYPE, RDB$PARAMETER_NUMBER;

-- sprawdzić trailing spaces w nazwach
SELECT '[' || RDB$PROCEDURE_NAME || ']' FROM RDB$PROCEDURES;
```
