using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaRestaurante.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MultiRestaurant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ---------- Datos existentes: todo pasa al primer restaurante ----------
            // Si hay datos pero ningún restaurante, se crea uno para poder asignarlos.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Restaurants)
   AND (EXISTS (SELECT 1 FROM Customers) OR EXISTS (SELECT 1 FROM Users))
    INSERT INTO Restaurants (Name, Address, PhoneNumber, Email, OpeningHours, CreatedAt, UpdatedAt, IsActive)
    VALUES (N'Mi restaurante', N'', N'', N'', NULL, SYSUTCDATETIME(), NULL, 1);");

            // Las reservas toman el restaurante de su mesa.
            migrationBuilder.Sql(@"
UPDATE r SET r.RestaurantId = t.RestaurantId
FROM Reservations r INNER JOIN Tables t ON t.Id = r.TableId;");

            migrationBuilder.Sql(@"
UPDATE Customers SET RestaurantId = (SELECT MIN(Id) FROM Restaurants) WHERE RestaurantId = 0;");

            // El personal que ya existía pertenece al primer restaurante.
            migrationBuilder.Sql(@"
UPDATE Users SET RestaurantId = (SELECT MIN(Id) FROM Restaurants)
WHERE RestaurantId IS NULL AND Role <> N'PlatformAdmin';");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RestaurantId",
                table: "Users",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RestaurantId_ReservationDateTime",
                table: "Reservations",
                columns: new[] { "RestaurantId", "ReservationDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_RestaurantId_Email",
                table: "Customers",
                columns: new[] { "RestaurantId", "Email" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Restaurants_RestaurantId",
                table: "Customers",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Restaurants_RestaurantId",
                table: "Reservations",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Restaurants_RestaurantId",
                table: "Users",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Restaurants_RestaurantId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Restaurants_RestaurantId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Restaurants_RestaurantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RestaurantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RestaurantId_ReservationDateTime",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Customers_RestaurantId_Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);
        }
    }
}
