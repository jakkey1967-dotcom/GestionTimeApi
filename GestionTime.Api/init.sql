-- Initialization script for development database
-- This script will be executed when PostgreSQL container starts for the first time

-- Create the gestiontime schema
CREATE SCHEMA IF NOT EXISTS gestiontime;

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA gestiontime TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA gestiontime TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA gestiontime TO postgres;

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Log the initialization
\echo 'GestionTime database initialized successfully'