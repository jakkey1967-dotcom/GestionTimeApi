-- ============================================================================
-- Create user_profiles table in gestiontime schema
-- ============================================================================

CREATE TABLE IF NOT EXISTS gestiontime.user_profiles (
    id UUID PRIMARY KEY REFERENCES gestiontime.users(id) ON DELETE CASCADE,
    
    -- Personal data
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    phone VARCHAR(20),
    mobile VARCHAR(20),
    
    -- Address
    address VARCHAR(200),
    city VARCHAR(100),
    postal_code VARCHAR(10),
    
    -- Work information
    department VARCHAR(100),
    position VARCHAR(100),
    employee_type VARCHAR(50), -- tecnico, tecnico_remoto, atencion_cliente, administrativo, manager
    hire_date DATE,
    
    -- Others
    avatar_url VARCHAR(500),
    notes TEXT,
    
    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now()
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_user_profiles_employee_type ON gestiontime.user_profiles(employee_type);
CREATE INDEX IF NOT EXISTS idx_user_profiles_department ON gestiontime.user_profiles(department);

-- Verification
SELECT table_name, column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'gestiontime' AND table_name = 'user_profiles'
ORDER BY ordinal_position;