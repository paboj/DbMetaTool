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
- `RDB$PROCEDURE_PARAMETERS` — **potwierdzone testem 2026-08-11** (`scratchpad/FbConnTest/`
  przeciw `GET_EXPIRING_STOCK_ITEMS`, ma parametr z defaultem — dobry przypadek testowy):
  - `RDB$PARAMETER_TYPE`: `0`=input, `1`=output (`Int16`).
  - `RDB$PARAMETER_NUMBER`: `Int16`, numeracja **osobna per kierunek** (input 0,1,...;
    output znów od 0) — nie globalna pozycja w całej liście parametrów.
  - `RDB$FIELD_SOURCE`: **zawsze** nazwa domeny (`Int16`→`String`, trailing spaces) —
    nawet gdy parametr zadeklarowany inline (`DAYS INTEGER`), Firebird tworzy ukrytą
    systemową domenę (`RDB$8`, `RDB$9`, ...). Żeby dostać faktyczny typ SQL, trzeba
    dociągnąć `RDB$FIELDS` po `RDB$FIELD_SOURCE = RDB$FIELDS.RDB$FIELD_NAME` i czytać
    stamtąd `RDB$FIELD_TYPE`/długość/precyzję/skalę — nie da się użyć samego
    `RDB$FIELD_SOURCE` jako tekstu typu.
  - `RDB$DEFAULT_SOURCE`: `String` z prefiksem `"= "` (np. `"= 0"` dla
    `INCLUDE_OPENED SMALLINT = 0`), `NULL`/`DBNull` gdy brak defaultu. Inaczej niż
    `DomainDefinition.DefaultValue` (surowy literał bez słowa kluczowego) — przy
    ekstrakcji do `ProcedureParameter.DefaultValue` trzeba zdjąć prefiks `"= "` dla
    spójności konwencji w modelu.
  - `RDB$PARAMETER_MECHANISM`: `Int16`, `0` w obu testowanych parametrach (standardowy
    mechanizm by-value) — nieużywane w uproszczonym modelu, tylko odnotowane.
- Różnice względem SQL Server/Postgres nie są oczywiste — nie zakładać zachowania na
  podstawie doświadczenia z innych baz, weryfikować (IBExpert albo szybki test przez
  driver, jak wyżej — driver bywa dokładniejszy, bo pokazuje, jak dane trafiają do C#,
  nie tylko jak wyglądają w GUI).

## Zapytania weryfikacyjne

Format źródła i trailing spaces (`RDB$PROCEDURE_NAME`, `RDB$PROCEDURE_SOURCE`) oraz
parametry procedur (`RDB$PROCEDURE_PARAMETERS`) — zweryfikowane, patrz wyżej. Zapytanie
użyte do weryfikacji (rozszerzone o `RDB$DEFAULT_SOURCE`, `RDB$PARAMETER_MECHANISM`
względem pierwotnej wersji poniżej):

```sql
-- Parametry procedur: IN/OUT (0=input,1=output), typ, pozycja
SELECT RDB$PROCEDURE_NAME, RDB$PARAMETER_NAME, RDB$PARAMETER_TYPE,
       RDB$PARAMETER_NUMBER, RDB$FIELD_SOURCE
FROM RDB$PROCEDURE_PARAMETERS
ORDER BY RDB$PROCEDURE_NAME, RDB$PARAMETER_TYPE, RDB$PARAMETER_NUMBER;
```
