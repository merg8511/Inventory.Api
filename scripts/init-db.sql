-- Inventory API Database Initialization Script
-- This script creates the schema and sets up the database

-- Create schema
CREATE SCHEMA IF NOT EXISTS inventory;

-- Set search path
SET search_path TO inventory;

-- Grant permissions (adjust as needed for your environment)
-- GRANT ALL PRIVILEGES ON SCHEMA inventory TO your_app_user;

-- Note: Tables will be created by EF Core migrations
-- Run: dotnet ef database update --project src/Inventory.Infrastructure --startup-project src/Inventory.Api

-- Sample seed data for testing (uncomment if needed)
/*
-- Insert default tenant
INSERT INTO inventory.tenants (id, name, created_at)
VALUES ('00000000-0000-0000-0000-000000000001', 'Default Tenant', NOW());

-- Insert default units of measure
INSERT INTO inventory.units_of_measure (id, tenant_id, code, name, is_active, created_at, created_by)
VALUES 
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'EA', 'Each', true, NOW(), 'system'),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'BOX', 'Box', true, NOW(), 'system'),
    (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'PCS', 'Pieces', true, NOW(), 'system');
*/
