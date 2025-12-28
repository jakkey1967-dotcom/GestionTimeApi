-- ============================================================================
-- Fix BCrypt password hashes with correct format
-- ============================================================================

-- Update passwords with correct BCrypt hashes
UPDATE gestiontime.users 
SET password_hash = '$2a$11$F2rXei6DP86zLECPxfPCiu9RfwEJ5BriolS2Nm.wZxhF1zaMcAqre' 
WHERE email = 'psantos@global-retail.com';

UPDATE gestiontime.users 
SET password_hash = '$2a$11$nMh76RSEjQqKPL/r3kQoa.SuLcTsxDLr3UH3ptH3JOE7buR0f6i2e' 
WHERE email = 'admin@gestiontime.local';

UPDATE gestiontime.users 
SET password_hash = '$2a$11$gJsKzwrvji.abnZBghLVJujEyZUqt3rqjPwcGOAMmQqvWColJfaPK' 
WHERE email = 'tecnico1@global-retail.com';

-- Verification - show first 10 chars of hash to verify format
SELECT 
    email,
    LENGTH(password_hash) as hash_length,
    SUBSTRING(password_hash, 1, 10) as hash_start
FROM gestiontime.users 
ORDER BY email;