using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatiesGarden.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // All DDL uses IF NOT EXISTS / IF EXISTS guards so this migration is fully
            // idempotent: safe to run on both fresh databases and on pre-existing
            // databases that were created before EF migrations were introduced.
            // After this migration runs, __EFMigrationsHistory records it — subsequent
            // runs skip it entirely.
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                CREATE TABLE IF NOT EXISTS subscribers (
                    "Id"           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "Email"        varchar(254) NOT NULL,
                    "FirstName"    varchar(100),
                    "SubscribedAt" timestamptz NOT NULL DEFAULT now()
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_subscribers_Email" ON subscribers ("Email");

                CREATE TABLE IF NOT EXISTS collections (
                    "Id"           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "Title"        varchar(200) NOT NULL,
                    "Slug"         varchar(200) NOT NULL,
                    "Description"  varchar(2000) NOT NULL DEFAULT '',
                    "CoverImageUrl" text,
                    "IsActive"     boolean NOT NULL DEFAULT true,
                    "DisplayOrder" integer NOT NULL DEFAULT 0,
                    "StartDate"    timestamptz NOT NULL DEFAULT now(),
                    "EndDate"      timestamptz,
                    "CreatedAt"    timestamptz NOT NULL DEFAULT now()
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_collections_Slug" ON collections ("Slug");

                CREATE TABLE IF NOT EXISTS products (
                    "Id"              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "Name"            varchar(200) NOT NULL,
                    "Slug"            varchar(200) NOT NULL,
                    "Description"     varchar(2000) NOT NULL DEFAULT '',
                    "Price"           numeric(10,2) NOT NULL DEFAULT 0,
                    "StockQuantity"   integer,
                    "IsAvailable"     boolean NOT NULL DEFAULT true,
                    "CanLocalDeliver" boolean NOT NULL DEFAULT true,
                    "ImageUrls"       text[] NOT NULL DEFAULT '{}',
                    "CollectionId"    uuid REFERENCES collections("Id") ON DELETE SET NULL,
                    "HowToBuyNote"    varchar(500),
                    "DisplayOrder"    integer NOT NULL DEFAULT 0,
                    "CreatedAt"       timestamptz NOT NULL DEFAULT now(),
                    "StripeProductId" text,
                    "StripePriceId"   text
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_products_Slug" ON products ("Slug");
                CREATE INDEX IF NOT EXISTS "IX_products_CollectionId" ON products ("CollectionId");

                CREATE TABLE IF NOT EXISTS orders (
                    "Id"                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "OrderNumber"              varchar(20) NOT NULL,
                    "CustomerFirstName"        varchar(100) NOT NULL,
                    "CustomerLastName"         varchar(100) NOT NULL,
                    "CustomerEmail"            varchar(254) NOT NULL,
                    "CustomerPhone"            varchar(30) NOT NULL,
                    "DeliveryType"             integer NOT NULL DEFAULT 0,
                    "DeliveryAddress"          varchar(500),
                    "DeliveryPostcode"         varchar(10),
                    "CustomerNotes"            varchar(1000),
                    "Subtotal"                 numeric(10,2) NOT NULL DEFAULT 0,
                    "DeliveryFee"              numeric(10,2) NOT NULL DEFAULT 0,
                    "Total"                    numeric(10,2) NOT NULL DEFAULT 0,
                    "Status"                   integer NOT NULL DEFAULT 0,
                    "StripeSessionId"          text,
                    "StripePaymentIntentId"    text,
                    "AdminNotes"               varchar(2000),
                    "CustomerId"               varchar(100),
                    "CustomerIdentityProvider" varchar(50),
                    "OrchestrationInstanceId"  varchar(200),
                    "CreatedAt"                timestamptz NOT NULL DEFAULT now(),
                    "UpdatedAt"                timestamptz NOT NULL DEFAULT now()
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_orders_OrderNumber" ON orders ("OrderNumber");
                CREATE INDEX IF NOT EXISTS "IX_orders_CustomerId" ON orders ("CustomerId");

                -- Add columns to pre-existing orders table (no-op if already present)
                ALTER TABLE orders ADD COLUMN IF NOT EXISTS "CustomerId"               varchar(100);
                ALTER TABLE orders ADD COLUMN IF NOT EXISTS "CustomerIdentityProvider" varchar(50);
                ALTER TABLE orders ADD COLUMN IF NOT EXISTS "OrchestrationInstanceId"  varchar(200);

                CREATE TABLE IF NOT EXISTS order_lines (
                    "Id"              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "OrderId"         uuid NOT NULL REFERENCES orders("Id") ON DELETE CASCADE,
                    "ProductId"       uuid NOT NULL REFERENCES products("Id") ON DELETE RESTRICT,
                    "ProductName"     varchar(200) NOT NULL,
                    "ProductImageUrl" text,
                    "UnitPrice"       numeric(10,2) NOT NULL DEFAULT 0,
                    "Quantity"        integer NOT NULL DEFAULT 1,
                    "LineTotal"       numeric(10,2) NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS "IX_order_lines_OrderId" ON order_lines ("OrderId");
                CREATE INDEX IF NOT EXISTS "IX_order_lines_ProductId" ON order_lines ("ProductId");

                CREATE TABLE IF NOT EXISTS delivery_settings (
                    "Id"                      integer PRIMARY KEY DEFAULT 1,
                    "LocalDeliveryFee"        numeric(10,2) NOT NULL DEFAULT 5.00,
                    "FreeDeliveryThreshold"   numeric(10,2),
                    "DeliveryAreaDescription" varchar(500) NOT NULL DEFAULT '',
                    "CollectionAddress"       varchar(300) NOT NULL DEFAULT '',
                    "CollectionInstructions"  varchar(1000) NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS push_subscriptions (
                    "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "Endpoint"  varchar(500) NOT NULL,
                    "P256dh"    varchar(200) NOT NULL,
                    "Auth"      varchar(100) NOT NULL,
                    "CreatedAt" timestamptz NOT NULL DEFAULT now()
                );
                CREATE INDEX IF NOT EXISTS "IX_push_subscriptions_Endpoint" ON push_subscriptions ("Endpoint");

                CREATE TABLE IF NOT EXISTS order_status_history (
                    "Id"        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "OrderId"   uuid NOT NULL REFERENCES orders("Id") ON DELETE CASCADE,
                    "Status"    integer NOT NULL,
                    "Note"      varchar(500),
                    "ChangedBy" varchar(254),
                    "ChangedAt" timestamptz NOT NULL DEFAULT now()
                );
                CREATE INDEX IF NOT EXISTS "IX_order_status_history_OrderId_ChangedAt"
                    ON order_status_history ("OrderId", "ChangedAt");

                CREATE TABLE IF NOT EXISTS audit_logs (
                    "Id"         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    "Timestamp"  timestamptz NOT NULL DEFAULT now(),
                    "Action"     varchar(100) NOT NULL,
                    "EntityType" varchar(100) NOT NULL,
                    "EntityId"   varchar(100) NOT NULL,
                    "ActorEmail" varchar(254),
                    "ActorName"  varchar(200),
                    "Details"    text
                );
                CREATE INDEX IF NOT EXISTS "IX_audit_logs_Timestamp" ON audit_logs ("Timestamp");
                CREATE INDEX IF NOT EXISTS "IX_audit_logs_EntityType_EntityId" ON audit_logs ("EntityType", "EntityId");

                CREATE TABLE IF NOT EXISTS stripe_processed_events (
                    "EventId"     varchar(100) PRIMARY KEY,
                    "ProcessedAt" timestamptz NOT NULL DEFAULT now()
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS stripe_processed_events;
                DROP TABLE IF EXISTS audit_logs;
                DROP TABLE IF EXISTS order_status_history;
                DROP TABLE IF EXISTS push_subscriptions;
                DROP TABLE IF EXISTS delivery_settings;
                DROP TABLE IF EXISTS order_lines;
                DROP TABLE IF EXISTS order_status_history;
                DROP TABLE IF EXISTS products;
                DROP TABLE IF EXISTS subscribers;
                DROP TABLE IF EXISTS orders;
                DROP TABLE IF EXISTS collections;
                """);
        }
    }
}
