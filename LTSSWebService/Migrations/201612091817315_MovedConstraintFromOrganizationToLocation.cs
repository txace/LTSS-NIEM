namespace LTSSWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class MovedConstraintFromOrganizationToLocation : DbMigration
    {
        public override void Down()
        {
            DropForeignKey("dbo.Location", "OrganizationID", "dbo.Organization");
            AddColumn("dbo.Organization", "LocationID", c => c.Int(nullable: false));
            AddForeignKey("dbo.Organization", "LocationID", "dbo.Location");
            CreateIndex("dbo.Organization", "LocationID");
            DropIndex("dbo.Location", new[] { "OrganizationID" });
            DropColumn("dbo.Location", "OrganizationID");
        }

        public override void Up()
        {
            AddColumn("dbo.Location", "OrganizationID", c => c.Int(nullable: false));
            CreateIndex("dbo.Location", "OrganizationID");
            DropIndex("dbo.Organization", new[] { "LocationID" });
            DropForeignKey("dbo.Organization", "LocationID", "dbo.Location");
            DropColumn("dbo.Organization", "LocationID");
            AddForeignKey("dbo.Location", "OrganizationID", "dbo.Organization");
        }
    }
}