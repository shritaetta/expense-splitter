using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseSplitter.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SplitTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SplitTemplates_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Settlements_Users_PayeeId",
                        column: x => x.PayeeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Settlements_Users_PayerId",
                        column: x => x.PayerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", nullable: false),
                    NextRunDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SplitTemplateId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringExpenses_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringExpenses_SplitTemplates_SplitTemplateId",
                        column: x => x.SplitTemplateId,
                        principalTable: "SplitTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SplitTemplateItems",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShareValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitTemplateItems", x => new { x.TemplateId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SplitTemplateItems_SplitTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "SplitTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SplitTemplateItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SplitTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecurringExpenseId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Expenses_RecurringExpenses_RecurringExpenseId",
                        column: x => x.RecurringExpenseId,
                        principalTable: "RecurringExpenses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Expenses_Users_PayerId",
                        column: x => x.PayerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseParticipants",
                columns: table => new
                {
                    ExpenseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShareValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseParticipants", x => new { x.ExpenseId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ExpenseParticipants_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpenseParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseParticipants_UserId",
                table: "ExpenseParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_GroupId",
                table: "Expenses",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PayerId",
                table: "Expenses",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_RecurringExpenseId",
                table: "Expenses",
                column: "RecurringExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_GroupId",
                table: "RecurringExpenses",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_SplitTemplateId",
                table: "RecurringExpenses",
                column: "SplitTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_GroupId",
                table: "Settlements",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_PayeeId",
                table: "Settlements",
                column: "PayeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_PayerId",
                table: "Settlements",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SplitTemplateItems_UserId",
                table: "SplitTemplateItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SplitTemplates_GroupId",
                table: "SplitTemplates",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseParticipants");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "Settlements");

            migrationBuilder.DropTable(
                name: "SplitTemplateItems");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "RecurringExpenses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "SplitTemplates");

            migrationBuilder.DropTable(
                name: "Groups");
        }
    }
}
