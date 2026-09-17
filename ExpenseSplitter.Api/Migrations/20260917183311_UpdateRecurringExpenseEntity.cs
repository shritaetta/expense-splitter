using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseSplitter.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecurringExpenseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringExpenses_SplitTemplates_SplitTemplateId",
                table: "RecurringExpenses");

            migrationBuilder.RenameColumn(
                name: "CronExpression",
                table: "RecurringExpenses",
                newName: "StartDate");

            migrationBuilder.AlterColumn<Guid>(
                name: "SplitTemplateId",
                table: "RecurringExpenses",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "RecurringExpenses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "RecurringExpenses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PayerId",
                table: "RecurringExpenses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "SplitTypeId",
                table: "RecurringExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_PayerId",
                table: "RecurringExpenses",
                column: "PayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringExpenses_SplitTemplates_SplitTemplateId",
                table: "RecurringExpenses",
                column: "SplitTemplateId",
                principalTable: "SplitTemplates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringExpenses_Users_PayerId",
                table: "RecurringExpenses",
                column: "PayerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringExpenses_SplitTemplates_SplitTemplateId",
                table: "RecurringExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringExpenses_Users_PayerId",
                table: "RecurringExpenses");

            migrationBuilder.DropIndex(
                name: "IX_RecurringExpenses_PayerId",
                table: "RecurringExpenses");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "RecurringExpenses");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "RecurringExpenses");

            migrationBuilder.DropColumn(
                name: "PayerId",
                table: "RecurringExpenses");

            migrationBuilder.DropColumn(
                name: "SplitTypeId",
                table: "RecurringExpenses");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "RecurringExpenses",
                newName: "CronExpression");

            migrationBuilder.AlterColumn<Guid>(
                name: "SplitTemplateId",
                table: "RecurringExpenses",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringExpenses_SplitTemplates_SplitTemplateId",
                table: "RecurringExpenses",
                column: "SplitTemplateId",
                principalTable: "SplitTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
