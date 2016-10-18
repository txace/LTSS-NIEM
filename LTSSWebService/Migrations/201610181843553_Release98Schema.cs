namespace LTSSWebService.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Release98Schema : DbMigration
    {
        public override void Down()
        {
            DropColumn("dbo.Screening", "PersonVeteranMilitaryIndicator");
            DropColumn("dbo.Screening", "PersonActiveMilitaryIndicator");
        }

        public override void Up()
        {
            AddColumn("dbo.Screening", "PersonActiveMilitaryIndicator", c => c.String());
            AddColumn("dbo.Screening", "PersonVeteranMilitaryIndicator", c => c.String());
        }
    }
}