namespace KatiesGarden.Api.Data;

/// <summary>
/// Idempotent CREATE TABLE IF NOT EXISTS statements for all store tables.
/// Runs on every cold start after EnsureCreated() to handle pre-existing databases
/// that were created before the store tables were added.
/// </summary>
public static class SqlMigrations
{
    public const string EnsureNewTablesExist = """
        CREATE EXTENSION IF NOT EXISTS pgcrypto;

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
        CREATE UNIQUE INDEX IF NOT EXISTS ix_collections_slug ON collections ("Slug");

        CREATE TABLE IF NOT EXISTS products (
            "Id"             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            "Name"           varchar(200) NOT NULL,
            "Slug"           varchar(200) NOT NULL,
            "Description"    varchar(2000) NOT NULL DEFAULT '',
            "Price"          numeric(10,2) NOT NULL DEFAULT 0,
            "StockQuantity"  integer,
            "IsAvailable"    boolean NOT NULL DEFAULT true,
            "CanLocalDeliver" boolean NOT NULL DEFAULT true,
            "ImageUrls"      text[] NOT NULL DEFAULT '{}',
            "CollectionId"   uuid REFERENCES collections("Id") ON DELETE SET NULL,
            "HowToBuyNote"   varchar(500),
            "DisplayOrder"   integer NOT NULL DEFAULT 0,
            "CreatedAt"      timestamptz NOT NULL DEFAULT now(),
            "StripeProductId" text,
            "StripePriceId"  text
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_products_slug ON products ("Slug");

        CREATE TABLE IF NOT EXISTS orders (
            "Id"                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            "OrderNumber"           varchar(20) NOT NULL,
            "CustomerFirstName"     varchar(100) NOT NULL,
            "CustomerLastName"      varchar(100) NOT NULL,
            "CustomerEmail"         varchar(254) NOT NULL,
            "CustomerPhone"         varchar(30) NOT NULL,
            "DeliveryType"          integer NOT NULL DEFAULT 0,
            "DeliveryAddress"       varchar(500),
            "DeliveryPostcode"      varchar(10),
            "CustomerNotes"         varchar(1000),
            "Subtotal"              numeric(10,2) NOT NULL DEFAULT 0,
            "DeliveryFee"           numeric(10,2) NOT NULL DEFAULT 0,
            "Total"                 numeric(10,2) NOT NULL DEFAULT 0,
            "Status"                integer NOT NULL DEFAULT 0,
            "StripeSessionId"       text,
            "StripePaymentIntentId" text,
            "AdminNotes"            varchar(2000),
            "CreatedAt"             timestamptz NOT NULL DEFAULT now(),
            "UpdatedAt"             timestamptz NOT NULL DEFAULT now()
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_orders_order_number ON orders ("OrderNumber");

        CREATE TABLE IF NOT EXISTS order_lines (
            "Id"             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            "OrderId"        uuid NOT NULL REFERENCES orders("Id") ON DELETE CASCADE,
            "ProductId"      uuid NOT NULL,
            "ProductName"    varchar(200) NOT NULL,
            "ProductImageUrl" text,
            "UnitPrice"      numeric(10,2) NOT NULL DEFAULT 0,
            "Quantity"       integer NOT NULL DEFAULT 1,
            "LineTotal"      numeric(10,2) NOT NULL DEFAULT 0
        );

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
        CREATE INDEX IF NOT EXISTS ix_push_subscriptions_endpoint ON push_subscriptions ("Endpoint");
        """;
}
