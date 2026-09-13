using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Store.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCozyProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "A set of 5 vinyl stickers, perfect for decorating a laptop, water bottle, or journal.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "A hard enamel pin, 3cm, with a butterfly clasp back - a little something for your jacket or tote.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "A 350ml ceramic mug for your morning coffee or afternoon tea, dishwasher and microwave safe.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "A heavyweight cotton canvas tote bag, roomy enough for groceries, books, or everyday errands.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "An A5 dot grid notebook, 120 pages - good for journaling, planning, or sketching.");

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Currency", "Description", "ImageUrl", "IsActive", "Name", "PriceCents", "Slug", "StockQuantity" },
                values: new object[,]
                {
                    { 7, "aud", "An oversized, soft knit throw blanket - the kind you fight over on movie night.", "https://picsum.photos/seed/knit-throw/600/400", true, "Chunky Knit Throw", 4500, "chunky-knit-throw", 35 },
                    { 8, "aud", "A hand-poured soy candle in a warm vanilla and cedar scent, roughly 40 hours of burn time.", "https://picsum.photos/seed/candle/600/400", true, "Vanilla Cedar Candle", 2800, "vanilla-cedar-candle", 45 },
                    { 9, "aud", "A set of 4 handmade ceramic coasters with a soft matte glaze finish.", "https://picsum.photos/seed/coasters/600/400", true, "Ceramic Coaster Set", 1800, "ceramic-coaster-set", 55 },
                    { 10, "aud", "A calming loose leaf chamomile and lavender blend, 100g tin, good for winding down.", "https://picsum.photos/seed/tea-tin/600/400", true, "Chamomile Lavender Tea", 1600, "chamomile-lavender-tea", 70 },
                    { 11, "aud", "A plush reading pillow with arms and back support, for curling up with a good book.", "https://picsum.photos/seed/reading-pillow/600/400", true, "Reading Pillow", 5200, "reading-pillow", 25 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "A set of 5 vinyl stickers.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "A hard enamel pin, 3cm, with a butterfly clasp back.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "A 350ml ceramic mug, dishwasher and microwave safe.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "A heavyweight cotton canvas tote bag.");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "An A5 dot grid notebook, 120 pages.");
        }
    }
}
