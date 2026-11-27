using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaRestaurante.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class OrdersAndStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Restaurants",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "COP");

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedTipPercent",
                table: "Restaurants",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<int>(
                name: "Allergens",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StationId",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    TableId = table.Column<int>(type: "int", nullable: false),
                    WaiterId = table.Column<int>(type: "int", nullable: false),
                    ReservationId = table.Column<int>(type: "int", nullable: true),
                    Guests = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedById = table.Column<int>(type: "int", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TipAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_WaiterId",
                        column: x => x.WaiterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PrinterAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stations_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Course = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledById = table.Column<int>(type: "int", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Users_CancelledById",
                        column: x => x.CancelledById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_StationId",
                table: "MenuItems",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_CancelledById",
                table: "OrderItems",
                column: "CancelledById");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MenuItemId",
                table: "OrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ClientRequestId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ClientRequestId" },
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_RestaurantId_StationId_Status",
                table: "OrderItems",
                columns: new[] { "RestaurantId", "StationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_StationId",
                table: "OrderItems",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClosedById",
                table: "Orders",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OneOpenPerTable",
                table: "Orders",
                column: "TableId",
                unique: true,
                filter: "[Status] = 0 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReservationId",
                table: "Orders",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RestaurantId_ClosedAt",
                table: "Orders",
                columns: new[] { "RestaurantId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RestaurantId_Status",
                table: "Orders",
                columns: new[] { "RestaurantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_WaiterId",
                table: "Orders",
                column: "WaiterId");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_RestaurantId_SortOrder",
                table: "Stations",
                columns: new[] { "RestaurantId", "SortOrder" });

            // Restaurantes que ya existían: zonas Cocina y Barra, y cada plato a la suya
            // (bebidas a la Barra, el resto a la Cocina) antes de exigir la relación.
            migrationBuilder.Sql(@"
INSERT INTO Stations (Name, Type, SortOrder, PrinterAddress, RestaurantId, CreatedAt, UpdatedAt, IsActive)
SELECT N'Cocina', 0, 1, NULL, r.Id, SYSUTCDATETIME(), NULL, 1 FROM Restaurants r;

INSERT INTO Stations (Name, Type, SortOrder, PrinterAddress, RestaurantId, CreatedAt, UpdatedAt, IsActive)
SELECT N'Barra', 1, 2, NULL, r.Id, SYSUTCDATETIME(), NULL, 1 FROM Restaurants r;

UPDATE m SET StationId = s.Id
FROM MenuItems m
JOIN Stations s ON s.RestaurantId = m.RestaurantId
    AND s.Type = CASE WHEN m.Category = 3 THEN 1 ELSE 0 END;
");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Stations_StationId",
                table: "MenuItems",
                column: "StationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Stations_StationId",
                table: "MenuItems");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_StationId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "SuggestedTipPercent",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "Allergens",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "StationId",
                table: "MenuItems");
        }
    }
}
