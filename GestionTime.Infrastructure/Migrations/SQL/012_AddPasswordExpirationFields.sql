-- ============================================================================
-- Add password expiration fields to users table
-- ============================================================================

-- Add the new columns to the users table
ALTER TABLE gestiontime.users 
ADD COLUMN IF NOT EXISTS password_changed_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS must_change_password BOOLEAN NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS password_expiration_days INTEGER NOT NULL DEFAULT 90;

-- Initialize password_changed_at for existing users (so they don't immediately expire)
UPDATE gestiontime.users 
SET password_changed_at = NOW() 
WHERE password_changed_at IS NULL;

-- Verification
SELECT 
    email,
    password_changed_at,
    must_change_password,
    password_expiration_days
FROM gestiontime.users 
ORDER BY email;