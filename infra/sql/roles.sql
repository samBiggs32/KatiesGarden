-- ---------------------------------------------------------------------------
-- Least-privilege PostgreSQL roles for Katie's Garden
--
-- Run this script once as a superuser AFTER the initial EF Core migration
-- has created the schema. See docs/security/runbooks/db-least-privilege.md
-- for exact apply steps, validation, and rollback.
--
-- Two roles:
--   kg_migrate — DDL + DML, used ONLY by EF Core MigrateAsync() at startup.
--                Granted to the Neon branch user in DATABASE_URL_MIGRATE.
--   kg_app     — DML only, no schema changes. Used by the API at runtime.
--                INSERT-only on audit_logs (immutability: the app can write
--                audit entries but never update or delete them).
-- ---------------------------------------------------------------------------

-- ── kg_migrate ──────────────────────────────────────────────────────────────
-- Full schema access so EF Core can apply future migrations.
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'kg_migrate') THEN
    CREATE ROLE kg_migrate LOGIN PASSWORD 'CHANGE_ME_MIGRATE';
  END IF;
END$$;

GRANT CONNECT ON DATABASE katiesgarden TO kg_migrate;
GRANT USAGE, CREATE ON SCHEMA public TO kg_migrate;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO kg_migrate;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO kg_migrate;

-- Ensure future tables created by migrations are also accessible
ALTER DEFAULT PRIVILEGES IN SCHEMA public
  GRANT ALL PRIVILEGES ON TABLES TO kg_migrate;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
  GRANT ALL PRIVILEGES ON SEQUENCES TO kg_migrate;

-- ── kg_app ──────────────────────────────────────────────────────────────────
-- Runtime role: read + write on application tables, but NO DDL.
-- audit_logs is INSERT-only — the app can append but never mutate records.
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'kg_app') THEN
    CREATE ROLE kg_app LOGIN PASSWORD 'CHANGE_ME_APP';
  END IF;
END$$;

GRANT CONNECT ON DATABASE katiesgarden TO kg_app;
GRANT USAGE ON SCHEMA public TO kg_app;

-- All tables: SELECT + INSERT + UPDATE + DELETE (full DML, no DDL)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO kg_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO kg_app;

-- Revoke UPDATE and DELETE on audit_logs to enforce append-only immutability.
-- The real enforcement is this DB grant (not an ORM-layer check that code can bypass).
REVOKE UPDATE, DELETE ON audit_logs FROM kg_app;

-- Propagate to tables created by future migrations
ALTER DEFAULT PRIVILEGES IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO kg_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO kg_app;
