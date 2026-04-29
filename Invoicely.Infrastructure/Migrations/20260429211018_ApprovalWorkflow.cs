using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invoicely.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ReviewedByUserId",
                table: "Invoices",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_ReviewedByUserId",
                table: "Invoices",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_ReviewedByUserId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ReviewedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Invoices");
        }
    }
}
