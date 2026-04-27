using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vanilla.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(name: "Customers", columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
            }, constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

            migrationBuilder.CreateTable(name: "Orders", columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.ForeignKey(name: "FK_Orders_Customers_CustomerId", column: x => x.CustomerId, principalTable: "Customers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

            migrationBuilder.CreateTable(name: "Payments", columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(name: "FK_Payments_Customers_CustomerId", column: x => x.CustomerId, principalTable: "Customers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

            migrationBuilder.CreateIndex(name: "IX_Customers_IsDeleted_Name", table: "Customers", columns: new[] { "IsDeleted", "Name" });
            migrationBuilder.CreateIndex(name: "IX_Orders_CustomerId_IsDeleted_CreatedUtc", table: "Orders", columns: new[] { "CustomerId", "IsDeleted", "CreatedUtc" });
            migrationBuilder.CreateIndex(name: "IX_Payments_CustomerId_IsDeleted_CreatedUtc", table: "Payments", columns: new[] { "CustomerId", "IsDeleted", "CreatedUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Orders");
            migrationBuilder.DropTable(name: "Payments");
            migrationBuilder.DropTable(name: "Customers");
        }
    }
}