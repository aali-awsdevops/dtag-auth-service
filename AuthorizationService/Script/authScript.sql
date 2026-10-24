-- Create users table
-- Database: authdb

-- DROP DATABASE IF EXISTS authdb;

CREATE DATABASE authdb
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_India.1252'
    LC_CTYPE = 'English_India.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;
    
CREATE TABLE IF NOT EXISTS auth_users (
    id UUID PRIMARY KEY,
    customer_id int,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT,
    display_name TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now()
);

-- Create user permissions table
CREATE TABLE IF NOT EXISTS auth_user_permissions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
    permission TEXT NOT NULL,
    granted_at TIMESTAMP WITH TIME ZONE DEFAULT now()
);

-- Index to quickly lookup permissions by user
CREATE INDEX IF NOT EXISTS idx_auth_user_permissions_user_id ON auth_user_permissions(user_id);