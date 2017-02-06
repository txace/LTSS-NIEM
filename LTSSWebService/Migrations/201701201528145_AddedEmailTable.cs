namespace LTSSWebService.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedEmailTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Email",
                c => new
                    {
                        EmailID = c.Int(nullable: false, identity: true),
                        Body = c.String(nullable: false),
                        From = c.String(nullable: false),
                        LocationName = c.String(nullable: false),
                        Password = c.String(),
                        SmtpClient = c.String(nullable: false),
                        To = c.String(nullable: false),
                        UserName = c.String(),
                        UseSSL = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.EmailID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Email");
        }
    }
}
