-- ============================================================
-- Migration: Create cliente_notas table (global + personal)
-- Schema: pss_dvnx
-- Idempotent: safe to run multiple times
-- ============================================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'pss_dvnx' AND table_name = 'cliente_notas'
    ) THEN
        CREATE TABLE pss_dvnx.cliente_notas (
            id              uuid        NOT NULL DEFAULT gen_random_uuid(),
            cliente_id      int         NOT NULL,
            owner_user_id   uuid        NULL,
            nota            text        NOT NULL DEFAULT '',
            created_at      timestamptz NOT NULL DEFAULT now(),
            updated_at      timestamptz NOT NULL DEFAULT now(),
            created_by      uuid        NULL,
            updated_by      uuid        NULL,
            CONSTRAINT pk_cliente_notas PRIMARY KEY (id),
            CONSTRAINT fk_cliente_notas_cliente
                FOREIGN KEY (cliente_id) REFERENCES pss_dvnx.cliente(id),
            CONSTRAINT fk_cliente_notas_owner
                FOREIGN KEY (owner_user_id) REFERENCES pss_dvnx.users(id),
            CONSTRAINT fk_cliente_notas_created_by
                FOREIGN KEY (created_by) REFERENCES pss_dvnx.users(id),
            CONSTRAINT fk_cliente_notas_updated_by
                FOREIGN KEY (updated_by) REFERENCES pss_dvnx.users(id)
        );

        RAISE NOTICE 'Table pss_dvnx.cliente_notas created.';
    ELSE
        RAISE NOTICE 'Table pss_dvnx.cliente_notas already exists, skipping.';
    END IF;
END $$;

-- Unique index: one global note per client (owner_user_id IS NULL)
CREATE UNIQUE INDEX IF NOT EXISTS uq_cliente_notas_global
    ON pss_dvnx.cliente_notas (cliente_id)
    WHERE owner_user_id IS NULL;

-- Unique index: one personal note per client per user (owner_user_id IS NOT NULL)
CREATE UNIQUE INDEX IF NOT EXISTS uq_cliente_notas_personal
    ON pss_dvnx.cliente_notas (cliente_id, owner_user_id)
    WHERE owner_user_id IS NOT NULL;

-- General index on cliente_id
CREATE INDEX IF NOT EXISTS idx_cliente_notas_cliente_id
    ON pss_dvnx.cliente_notas (cliente_id);

RAISE NOTICE 'Migration complete: cliente_notas table and indexes ready.';
