-- =============================================================================
-- SCHEMA: inventory
-- =============================================================================
CREATE SCHEMA IF NOT EXISTS inventory;
-- =============================================================================
-- ENUMS
-- =============================================================================
CREATE TYPE inventory.tracking_type AS ENUM ('none', 'lot', 'serial');
CREATE TYPE inventory.transaction_type AS ENUM (
    'receipt', 'issue', 'positive_adj', 'negative_adj',
    'transfer_out', 'transfer_in', 'reserve', 'unreserve'
);
CREATE TYPE inventory.transfer_status AS ENUM (
    'draft', 'committed', 'in_transit', 'received', 'cancelled'
);
CREATE TYPE inventory.reservation_status AS ENUM (
    'active', 'confirmed', 'released', 'cancelled'
);
-- =============================================================================
-- REFERENCE TABLES
-- =============================================================================
CREATE TABLE inventory.units_of_measure (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    code VARCHAR(10) NOT NULL,
    name VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    
    CONSTRAINT uq_uom_tenant_code UNIQUE (tenant_id, code)
);
CREATE TABLE inventory.categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    name VARCHAR(100) NOT NULL,
    parent_category_id UUID REFERENCES inventory.categories(id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    
    CONSTRAINT uq_category_tenant_name UNIQUE (tenant_id, name, parent_category_id)
);
-- =============================================================================
-- CORE TABLES
-- =============================================================================
CREATE TABLE inventory.items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    sku VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    category_id UUID REFERENCES inventory.categories(id),
    unit_of_measure_id UUID NOT NULL REFERENCES inventory.units_of_measure(id),
    cost_price DECIMAL(18, 4) NOT NULL DEFAULT 0,
    sale_price DECIMAL(18, 4) NOT NULL DEFAULT 0,
    tracking_type inventory.tracking_type NOT NULL DEFAULT 'none',
    minimum_stock DECIMAL(18, 4),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    row_version INTEGER NOT NULL DEFAULT 1,
    
    CONSTRAINT uq_item_tenant_sku UNIQUE (tenant_id, sku),
    CONSTRAINT chk_item_cost_positive CHECK (cost_price >= 0),
    CONSTRAINT chk_item_price_positive CHECK (sale_price >= 0)
);
CREATE TABLE inventory.warehouses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    code VARCHAR(20) NOT NULL,
    name VARCHAR(100) NOT NULL,
    address VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    
    CONSTRAINT uq_warehouse_tenant_code UNIQUE (tenant_id, code)
);
CREATE TABLE inventory.locations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    code VARCHAR(30) NOT NULL,
    name VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    
    CONSTRAINT uq_location_warehouse_code UNIQUE (warehouse_id, code)
);
-- =============================================================================
-- INVENTORY CORE
-- =============================================================================
CREATE TABLE inventory.inventory_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    item_id UUID NOT NULL REFERENCES inventory.items(id),
    warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    location_id UUID REFERENCES inventory.locations(id),
    
    transaction_type inventory.transaction_type NOT NULL,
    quantity DECIMAL(18, 4) NOT NULL,
    unit_cost DECIMAL(18, 4),
    total_cost DECIMAL(18, 4),
    
    reference_type VARCHAR(50),
    reference_id VARCHAR(100),
    reason_code VARCHAR(30),
    reason_description VARCHAR(500),
    
    lot_number VARCHAR(50),
    serial_number VARCHAR(100),
    expiration_date DATE,
    
    transaction_date TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    correlation_id VARCHAR(100),
    idempotency_key VARCHAR(100),
    
    CONSTRAINT chk_transaction_quantity_positive CHECK (quantity > 0)
);
CREATE TABLE inventory.inventory_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    item_id UUID NOT NULL REFERENCES inventory.items(id),
    warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    location_id UUID REFERENCES inventory.locations(id),
    
    on_hand DECIMAL(18, 4) NOT NULL DEFAULT 0,
    reserved DECIMAL(18, 4) NOT NULL DEFAULT 0,
    in_transit DECIMAL(18, 4) NOT NULL DEFAULT 0,
    
    last_transaction_id UUID REFERENCES inventory.inventory_transactions(id),
    last_transaction_date TIMESTAMPTZ,
    
    row_version INTEGER NOT NULL DEFAULT 1,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uq_balance_tenant_item_wh_loc UNIQUE (tenant_id, item_id, warehouse_id, location_id),
    CONSTRAINT chk_balance_on_hand_positive CHECK (on_hand >= 0),
    CONSTRAINT chk_balance_reserved_positive CHECK (reserved >= 0),
    CONSTRAINT chk_balance_in_transit_positive CHECK (in_transit >= 0),
    CONSTRAINT chk_balance_reserved_lte_onhand CHECK (reserved <= on_hand)
);
-- =============================================================================
-- TRANSFERS
-- =============================================================================
CREATE TABLE inventory.transfers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    transfer_number VARCHAR(30) NOT NULL,
    
    source_warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    source_location_id UUID REFERENCES inventory.locations(id),
    destination_warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    destination_location_id UUID REFERENCES inventory.locations(id),
    
    status inventory.transfer_status NOT NULL DEFAULT 'draft',
    notes VARCHAR(1000),
    
    committed_at TIMESTAMPTZ,
    shipped_at TIMESTAMPTZ,
    received_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    row_version INTEGER NOT NULL DEFAULT 1,
    
    CONSTRAINT uq_transfer_tenant_number UNIQUE (tenant_id, transfer_number),
    CONSTRAINT chk_transfer_diff_warehouse CHECK (source_warehouse_id != destination_warehouse_id)
);
CREATE TABLE inventory.transfer_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transfer_id UUID NOT NULL REFERENCES inventory.transfers(id) ON DELETE CASCADE,
    item_id UUID NOT NULL REFERENCES inventory.items(id),
    
    requested_quantity DECIMAL(18, 4) NOT NULL,
    shipped_quantity DECIMAL(18, 4),
    received_quantity DECIMAL(18, 4),
    
    lot_number VARCHAR(50),
    serial_number VARCHAR(100),
    notes VARCHAR(500),
    
    CONSTRAINT chk_transfer_line_qty_positive CHECK (requested_quantity > 0)
);
-- =============================================================================
-- RESERVATIONS
-- =============================================================================
CREATE TABLE inventory.reservations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    item_id UUID NOT NULL REFERENCES inventory.items(id),
    warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    location_id UUID REFERENCES inventory.locations(id),
    
    quantity DECIMAL(18, 4) NOT NULL,
    
    order_type VARCHAR(50) NOT NULL,
    order_id VARCHAR(100) NOT NULL,
    
    status inventory.reservation_status NOT NULL DEFAULT 'active',
    expires_at TIMESTAMPTZ,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    updated_at TIMESTAMPTZ,
    updated_by VARCHAR(100),
    correlation_id VARCHAR(100),
    
    CONSTRAINT chk_reservation_qty_positive CHECK (quantity > 0)
);
-- =============================================================================
-- IDEMPOTENCY
-- =============================================================================
CREATE TABLE inventory.idempotency_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key VARCHAR(100) NOT NULL,
    tenant_id UUID NOT NULL,
    request_hash VARCHAR(64) NOT NULL,
    response_status_code INTEGER NOT NULL,
    response_body TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT uq_idempotency_tenant_key UNIQUE (tenant_id, key)
);
-- =============================================================================
-- INDEXES
-- =============================================================================
-- Items
CREATE INDEX ix_items_tenant_id ON inventory.items(tenant_id);
CREATE INDEX ix_items_tenant_category ON inventory.items(tenant_id, category_id) WHERE is_active = TRUE;
CREATE INDEX ix_items_tenant_sku_search ON inventory.items(tenant_id, sku) 
    INCLUDE (name, is_active);
-- Warehouses
CREATE INDEX ix_warehouses_tenant_id ON inventory.warehouses(tenant_id);
CREATE INDEX ix_warehouses_tenant_active ON inventory.warehouses(tenant_id) WHERE is_active = TRUE;
-- Locations
CREATE INDEX ix_locations_warehouse ON inventory.locations(warehouse_id);
CREATE INDEX ix_locations_tenant ON inventory.locations(tenant_id);
-- Inventory Transactions (Ledger)
CREATE INDEX ix_transactions_tenant_item ON inventory.inventory_transactions(tenant_id, item_id);
CREATE INDEX ix_transactions_tenant_wh ON inventory.inventory_transactions(tenant_id, warehouse_id);
CREATE INDEX ix_transactions_tenant_date ON inventory.inventory_transactions(tenant_id, transaction_date DESC);
CREATE INDEX ix_transactions_reference ON inventory.inventory_transactions(reference_type, reference_id) 
    WHERE reference_type IS NOT NULL;
CREATE INDEX ix_transactions_correlation ON inventory.inventory_transactions(correlation_id) 
    WHERE correlation_id IS NOT NULL;
-- Inventory Balances
CREATE INDEX ix_balances_tenant_item ON inventory.inventory_balances(tenant_id, item_id);
CREATE INDEX ix_balances_tenant_wh ON inventory.inventory_balances(tenant_id, warehouse_id);
CREATE INDEX ix_balances_low_stock ON inventory.inventory_balances(tenant_id, item_id) 
    WHERE (on_hand - reserved) > 0;
-- Transfers
CREATE INDEX ix_transfers_tenant ON inventory.transfers(tenant_id);
CREATE INDEX ix_transfers_tenant_status ON inventory.transfers(tenant_id, status);
CREATE INDEX ix_transfers_source_wh ON inventory.transfers(source_warehouse_id);
CREATE INDEX ix_transfers_dest_wh ON inventory.transfers(destination_warehouse_id);
-- Transfer Lines
CREATE INDEX ix_transfer_lines_transfer ON inventory.transfer_lines(transfer_id);
CREATE INDEX ix_transfer_lines_item ON inventory.transfer_lines(item_id);
-- Reservations
CREATE INDEX ix_reservations_tenant ON inventory.reservations(tenant_id);
CREATE INDEX ix_reservations_tenant_item ON inventory.reservations(tenant_id, item_id);
CREATE INDEX ix_reservations_tenant_order ON inventory.reservations(tenant_id, order_type, order_id);
CREATE INDEX ix_reservations_active ON inventory.reservations(tenant_id, item_id, warehouse_id) 
    WHERE status = 'active';
CREATE INDEX ix_reservations_expires ON inventory.reservations(expires_at) 
    WHERE status = 'active' AND expires_at IS NOT NULL;
-- Idempotency Keys
CREATE INDEX ix_idempotency_expires ON inventory.idempotency_keys(expires_at);
CREATE INDEX ix_idempotency_tenant_key ON inventory.idempotency_keys(tenant_id, key);
-- =============================================================================
-- COMMENTS
-- =============================================================================
COMMENT ON TABLE inventory.inventory_transactions IS 'Ledger/Kardex: Source of truth for all stock movements';
COMMENT ON TABLE inventory.inventory_balances IS 'Snapshot/Projection: Materialized balance for fast queries';
COMMENT ON COLUMN inventory.inventory_balances.on_hand IS 'Physical stock available in warehouse';
COMMENT ON COLUMN inventory.inventory_balances.reserved IS 'Stock reserved for pending orders';
COMMENT ON COLUMN inventory.inventory_balances.in_transit IS 'Stock being transferred to this warehouse';