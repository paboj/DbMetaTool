-- Wygenerowane z manual-test-data/tables.md — patrz 01-domains.sql

CREATE TABLE PRODUCT_DEFINITIONS (
  NAME  D_PRODUCT_NAME,
  UNIT  D_UNIT_TYPE
);

CREATE TABLE STOCK_ITEMS (
  ID               INTEGER,
  NAME             D_PRODUCT_NAME,
  AMOUNT           D_AMOUNT,
  LOCATION         D_STORAGE_LOCATION,
  EXPIRATION_DATE  DATE
);
