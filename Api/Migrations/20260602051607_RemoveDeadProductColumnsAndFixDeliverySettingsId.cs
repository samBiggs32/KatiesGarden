using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KatiesGarden.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeadProductColumnsAndFixDeliverySettingsId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "StripeProductId",
                table: "products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeProductId",
                table: "products",
                type: "text",
                nullable: true);
        }
    }
}
