using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WACAMS.Infrastructure.Migrations
{
    /// <summary>
    /// Initial migration for escalation workflow schema
    /// </summary>
    public partial class InitialEscalationSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create attendance_exceptions table
            migrationBuilder.CreateTable(
                name: "attendance_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    employee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    exception_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    exception_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scheduled_start = table.Column<TimeSpan>(type: "interval", nullable: true),
                    scheduled_end = table.Column<TimeSpan>(type: "interval", nullable: true),
                    actual_clock_in = table.Column<TimeSpan>(type: "interval", nullable: true),
                    actual_clock_out = table.Column<TimeSpan>(type: "interval", nullable: true),
                    approval_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    approval_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approval_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_exceptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_exceptions_employee_id",
                table: "attendance_exceptions",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_exceptions_exception_date",
                table: "attendance_exceptions",
                column: "exception_date");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_exceptions_exception_category",
                table: "attendance_exceptions",
                column: "exception_category");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_exceptions_approval_status",
                table: "attendance_exceptions",
                column: "approval_status");

            // Create escalations table
            migrationBuilder.CreateTable(
                name: "escalations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exception_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_to = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    action_taken_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    auto_closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_escalation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    document_uploaded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    document_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    document_uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    action_taken_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escalations", x => x.id);
                    table.ForeignKey(
                        name: "fk_escalations_exceptions",
                        column: x => x.exception_id,
                        principalTable: "attendance_exceptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_escalations_exception_id",
                table: "escalations",
                column: "exception_id");

            migrationBuilder.CreateIndex(
                name: "ix_escalations_employee_id",
                table: "escalations",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_escalations_status",
                table: "escalations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_escalations_next_escalation_time",
                table: "escalations",
                column: "next_escalation_time");

            migrationBuilder.CreateIndex(
                name: "ix_escalations_created_at",
                table: "escalations",
                column: "created_at");

            // Create escalation_history table
            migrationBuilder.CreateTable(
                name: "escalation_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escalation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    from_manager_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    from_manager_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    to_manager_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_manager_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escalation_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_escalation_history_escalations",
                        column: x => x.escalation_id,
                        principalTable: "escalations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_escalation_history_escalation_id",
                table: "escalation_history",
                column: "escalation_id");

            migrationBuilder.CreateIndex(
                name: "ix_escalation_history_timestamp",
                table: "escalation_history",
                column: "timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "escalation_history");
            migrationBuilder.DropTable(name: "escalations");
            migrationBuilder.DropTable(name: "attendance_exceptions");
        }
    }
}
