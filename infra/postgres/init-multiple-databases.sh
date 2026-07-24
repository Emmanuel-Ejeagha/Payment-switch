#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
  CREATE DATABASE "IdentityDb";
  CREATE DATABASE "MerchantDb";
  CREATE DATABASE "PaymentDb";
  CREATE DATABASE "LedgerDb";
  CREATE DATABASE "NotificationDb";
  CREATE DATABASE "SettlementDb";
EOSQL
