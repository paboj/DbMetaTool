-- Wygenerowane z manual-test-data/domains.md — uruchamiane automatycznie przez
-- firebirdsql/firebird przy pierwszym starcie kontenera (docker-entrypoint-initdb.d),
-- pod warunkiem że katalog danych jest pusty.

CREATE DOMAIN D_PRODUCT_NAME AS VARCHAR(100) NOT NULL;

CREATE DOMAIN D_AMOUNT AS DOUBLE PRECISION DEFAULT 0 NOT NULL
  CHECK (VALUE >= 0);

CREATE DOMAIN D_UNIT_TYPE AS SMALLINT DEFAULT 0 NOT NULL
  CHECK (VALUE BETWEEN 0 AND 3);

CREATE DOMAIN D_STORAGE_LOCATION AS SMALLINT DEFAULT 0 NOT NULL
  CHECK (VALUE BETWEEN 0 AND 3);
