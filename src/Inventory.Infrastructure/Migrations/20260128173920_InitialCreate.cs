using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_categories_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalSchema: "inventory",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_status_code = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units_of_measure",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units_of_measure", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tracking_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minimum_stock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_items_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "inventory",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_items_units_of_measure_unit_of_measure_id",
                        column: x => x.unit_of_measure_id,
                        principalSchema: "inventory",
                        principalTable: "units_of_measure",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_locations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reason_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reason_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lot_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    transaction_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_locations_location_id",
                        column: x => x.location_id,
                        principalSchema: "inventory",
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    order_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.id);
                    table.ForeignKey(
                        name: "FK_reservations_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservations_locations_location_id",
                        column: x => x.location_id,
                        principalSchema: "inventory",
                        principalTable: "locations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reservations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    committed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    shipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                    table.ForeignKey(
                        name: "FK_transfers_locations_destination_location_id",
                        column: x => x.destination_location_id,
                        principalSchema: "inventory",
                        principalTable: "locations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transfers_locations_source_location_id",
                        column: x => x.source_location_id,
                        principalSchema: "inventory",
                        principalTable: "locations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transfers_warehouses_destination_warehouse_id",
                        column: x => x.destination_warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_warehouses_source_warehouse_id",
                        column: x => x.source_warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_balances",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    on_hand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    in_transit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    last_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_transaction_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_balances", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_balances_inventory_transactions_last_transaction_~",
                        column: x => x.last_transaction_id,
                        principalSchema: "inventory",
                        principalTable: "inventory_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventory_balances_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_balances_locations_location_id",
                        column: x => x.location_id,
                        principalSchema: "inventory",
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventory_balances_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    shipped_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    received_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    lot_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_transfer_lines_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "inventory",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_lines_transfers_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "inventory",
                        principalTable: "transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_category_id",
                schema: "inventory",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_keys_expires_at",
                schema: "inventory",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_keys_tenant_id_key",
                schema: "inventory",
                table: "idempotency_keys",
                columns: new[] { "tenant_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_item_id",
                schema: "inventory",
                table: "inventory_balances",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_last_transaction_id",
                schema: "inventory",
                table: "inventory_balances",
                column: "last_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_location_id",
                schema: "inventory",
                table: "inventory_balances",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_tenant_id_item_id",
                schema: "inventory",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_tenant_id_item_id_warehouse_id_location_~",
                schema: "inventory",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "item_id", "warehouse_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_tenant_id_warehouse_id",
                schema: "inventory",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_warehouse_id",
                schema: "inventory",
                table: "inventory_balances",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_correlation_id",
                schema: "inventory",
                table: "inventory_transactions",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_item_id",
                schema: "inventory",
                table: "inventory_transactions",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_location_id",
                schema: "inventory",
                table: "inventory_transactions",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_reference_type_reference_id",
                schema: "inventory",
                table: "inventory_transactions",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_tenant_id_item_id",
                schema: "inventory",
                table: "inventory_transactions",
                columns: new[] { "tenant_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_tenant_id_transaction_date",
                schema: "inventory",
                table: "inventory_transactions",
                columns: new[] { "tenant_id", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_tenant_id_warehouse_id",
                schema: "inventory",
                table: "inventory_transactions",
                columns: new[] { "tenant_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_warehouse_id",
                schema: "inventory",
                table: "inventory_transactions",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_category_id",
                schema: "inventory",
                table: "items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_tenant_id_category_id",
                schema: "inventory",
                table: "items",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_items_tenant_id_sku",
                schema: "inventory",
                table: "items",
                columns: new[] { "tenant_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_unit_of_measure_id",
                schema: "inventory",
                table: "items",
                column: "unit_of_measure_id");

            migrationBuilder.CreateIndex(
                name: "IX_locations_warehouse_id_code",
                schema: "inventory",
                table: "locations",
                columns: new[] { "warehouse_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservations_expires_at",
                schema: "inventory",
                table: "reservations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_item_id",
                schema: "inventory",
                table: "reservations",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_location_id",
                schema: "inventory",
                table: "reservations",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_tenant_id_item_id",
                schema: "inventory",
                table: "reservations",
                columns: new[] { "tenant_id", "item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_reservations_tenant_id_order_type_order_id",
                schema: "inventory",
                table: "reservations",
                columns: new[] { "tenant_id", "order_type", "order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_reservations_warehouse_id",
                schema: "inventory",
                table: "reservations",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_lines_item_id",
                schema: "inventory",
                table: "transfer_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_lines_transfer_id",
                schema: "inventory",
                table: "transfer_lines",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_destination_location_id",
                schema: "inventory",
                table: "transfers",
                column: "destination_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_destination_warehouse_id",
                schema: "inventory",
                table: "transfers",
                column: "destination_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_source_location_id",
                schema: "inventory",
                table: "transfers",
                column: "source_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_source_warehouse_id",
                schema: "inventory",
                table: "transfers",
                column: "source_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_tenant_id_status",
                schema: "inventory",
                table: "transfers",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_tenant_id_transfer_number",
                schema: "inventory",
                table: "transfers",
                columns: new[] { "tenant_id", "transfer_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_units_of_measure_tenant_id_code",
                schema: "inventory",
                table: "units_of_measure",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_tenant_id_code",
                schema: "inventory",
                table: "warehouses",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_balances",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "reservations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "transfer_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_transactions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "transfers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "locations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "units_of_measure",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "inventory");
        }
    }
}
