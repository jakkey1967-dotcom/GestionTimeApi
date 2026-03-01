-- ============================================================
-- SEED de prueba para client_versions (simula Desktop real)
-- Ejecutar ANTES de las pruebas, limpiar DESPUÉS
-- ============================================================

-- Usuario ADMIN psantos: versión VIEJA (1.8.0) → REQUIRED (< min 2.0.0)
INSERT INTO pss_dvnx.client_versions
    (user_id, platform, app_version_raw, ver_major, ver_minor, ver_patch, ver_prerelease, os_version, machine_name, logged_at)
VALUES
    ('b455821b-e481-4969-825d-817ee4e85184', 'Desktop', '1.8.0', 1, 8, 0, NULL,
     'Windows 11 Pro 23H2', 'PC-PSANTOS', now() - interval '1 day');

-- Usuario jtrasancos: versión OUTDATED (2.0.0 < latest 2.0.2-beta)
INSERT INTO pss_dvnx.client_versions
    (user_id, platform, app_version_raw, ver_major, ver_minor, ver_patch, ver_prerelease, os_version, machine_name, logged_at)
VALUES
    ('022d39a0-e55a-4663-9d26-934273d69b7b', 'Desktop', '2.0.0', 2, 0, 0, NULL,
     'Windows 10 Enterprise', 'PC-JTRASANCOS', now() - interval '2 days');

-- Usuario msanchez: versión OK (2.0.2-beta = latest)
INSERT INTO pss_dvnx.client_versions
    (user_id, platform, app_version_raw, ver_major, ver_minor, ver_patch, ver_prerelease, os_version, machine_name, logged_at)
VALUES
    ('ce25e2db-d34f-4353-b25e-2506d9e70f82', 'Desktop', '2.0.2-beta', 2, 0, 2, 'beta',
     'Windows 11 Home', 'PC-MSANCHEZ', now() - interval '3 hours');

-- Usuario omgarcia: versión OK pero INACTIVO (logged_at hace 3 semanas > inactive_weeks=2)
INSERT INTO pss_dvnx.client_versions
    (user_id, platform, app_version_raw, ver_major, ver_minor, ver_patch, ver_prerelease, os_version, machine_name, logged_at)
VALUES
    ('50621c22-fd70-43ee-85c3-d3c7aeec2365', 'Desktop', '2.0.2-beta', 2, 0, 2, 'beta',
     'Windows 10 Pro', 'PC-OMGARCIA', now() - interval '21 days');

-- wsanchez: NO inserto nada → será NEVER
-- admin@admin.com: disabled → no debería salir
