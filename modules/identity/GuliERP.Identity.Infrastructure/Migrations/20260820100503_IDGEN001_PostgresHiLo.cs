using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuliERP.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IDGEN001_PostgresHiLo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "gulierp_hilo_sequence",
                schema: "identity",
                incrementBy: 10);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    max_existing_id bigint;
                    desired_last_value bigint;
                BEGIN
                    SELECT GREATEST(
                        0,
                        COALESCE((SELECT MAX("Id") FROM identity.gulierp_tenant), 0),
                        COALESCE((SELECT MAX("Id") FROM identity.gulierp_company), 0),
                        COALESCE((SELECT MAX("Id") FROM identity.gulierp_plant), 0),
                        COALESCE((SELECT MAX("Id") FROM identity.gulierp_organization_unit), 0),
                        COALESCE((SELECT MAX("Id") FROM identity."AspNetUsers"), 0),
                        COALESCE((SELECT MAX("Id") FROM identity."AspNetRoles"), 0),
                        COALESCE((SELECT MAX("Id") FROM identity.gulierp_user_company_membership), 0),
                        COALESCE((SELECT MAX("Id") FROM identity.gulierp_user_organization_membership), 0),
                        COALESCE((SELECT MAX("Id") FROM identity.gulierp_user_role_assignment), 0)
                    )
                    INTO max_existing_id;

                    desired_last_value := ((max_existing_id / 10) + 2) * 10;

                    PERFORM setval(
                        'identity.gulierp_hilo_sequence'::regclass,
                        desired_last_value,
                        true);
                END $$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                schema: "identity",
                table: "AspNetUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                schema: "identity",
                table: "AspNetRoles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "gulierp_hilo_sequence",
                schema: "identity");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                schema: "identity",
                table: "AspNetUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                schema: "identity",
                table: "AspNetRoles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
