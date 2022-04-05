using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MathematicGameApi.Migrations
{
    public partial class addInviteFriendDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AddedDate",
                table: "InviteFriends",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "InviteFriends",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedDate",
                table: "InviteFriends");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "InviteFriends");
        }
    }
}
