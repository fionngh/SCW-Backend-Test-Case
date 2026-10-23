using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ProtectAccessEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION audit.reject_access_event_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit.access_events is append-only: % is not allowed', TG_OP
                        USING ERRCODE = 'restrict_violation';
                END;
                $$;
                """
            );

            migrationBuilder.Sql(
                """
                CREATE TRIGGER access_events_reject_update_delete
                BEFORE UPDATE OR DELETE ON audit.access_events
                FOR EACH ROW EXECUTE FUNCTION audit.reject_access_event_change();
                """
            );

            migrationBuilder.Sql(
                """
                CREATE TRIGGER access_events_reject_truncate
                BEFORE TRUNCATE ON audit.access_events
                FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_access_event_change();
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER access_events_reject_truncate ON audit.access_events;");
            migrationBuilder.Sql("DROP TRIGGER access_events_reject_update_delete ON audit.access_events;");
            migrationBuilder.Sql("DROP FUNCTION audit.reject_access_event_change();");
        }
    }
}
