# Firebird 5.0 — specyfika do pilnowania

- Filtrować obiekty systemowe: `RDB$RELATIONS.RDB$SYSTEM_FLAG = 1` (analogicznie
  `RDB$PROCEDURES` itp.) — inaczej eksport zaśmieci się obiektami systemowymi.
- `CREATE OR ALTER PROCEDURE` — bezpieczna aktualizacja bez DROP, zachowuje granty.
- `RDB$PROCEDURE_SOURCE` (`BLOB SUB_TYPE TEXT`) — **potwierdzone testem 2026-08-11**
  (`scratchpad/FbConnTest/` przeciw `manual-test.fdb`): `FirebirdSql.Data.FirebirdClient`
  zwraca go jako zwykły `System.String` przez `reader.GetValue()` — żadnej ręcznej obsługi
  blob-streamu nie trzeba. Treść to czyste `BEGIN...END` (tylko body procedury), bez
  nagłówka `CREATE PROCEDURE`/`RETURNS` i bez `SET TERM`/`^` — zgodne z założeniem
  `ProcedureDefinition.SourceBody` w modelu (`Metadata/ProcedureDefinition.cs`).
- Nazwy w tabelach `RDB$*` są typu `CHAR` — mają trailing spaces (**potwierdzone tym
  samym testem**), zawsze `.Trim()`.
- Sterownik: NuGet `FirebirdSql.Data.FirebirdClient` **10.3.4**, dodany do projektu, build
  bez ostrzeżeń. Tryb remote/client-server jest czysto zarządzany — **potwierdzone**:
  connection string bez `fbclient.dll`/`fbembed.dll` (sam `DataSource`/`Port`/`Database`)
  wystarczył do połączenia z kontenerem `fb-dbmetatool`.
- `RDB$PROCEDURE_PARAMETERS` (IN/OUT, typ, pozycja) — **jeszcze nie zweryfikowane**, patrz
  zapytanie niżej. Nie zakładać kształtu przed sprawdzeniem — potrzebne przed pisaniem
  ekstrakcji parametrów w `ExportScripts`.
- Różnice względem SQL Server/Postgres nie są oczywiste — nie zakładać zachowania na
  podstawie doświadczenia z innych baz, weryfikować (IBExpert albo szybki test przez
  driver, jak wyżej — driver bywa dokładniejszy, bo pokazuje, jak dane trafiają do C#,
  nie tylko jak wyglądają w GUI).

## Zapytania weryfikacyjne

Format źródła i trailing spaces (`RDB$PROCEDURE_NAME`, `RDB$PROCEDURE_SOURCE`) —
zweryfikowane, patrz wyżej. Parametry procedur — nadal do zrobienia, tym samym sposobem
(rozszerzyć `scratchpad/FbConnTest/` albo sprawdzić w IBExpert):

```sql
-- Parametry procedur: IN/OUT (0=input,1=output), typ, pozycja
SELECT RDB$PROCEDURE_NAME, RDB$PARAMETER_NAME, RDB$PARAMETER_TYPE,
       RDB$PARAMETER_NUMBER, RDB$FIELD_SOURCE
FROM RDB$PROCEDURE_PARAMETERS
ORDER BY RDB$PROCEDURE_NAME, RDB$PARAMETER_TYPE, RDB$PARAMETER_NUMBER;
```
