namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixCourseUnicodeColumns : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.courses", "description", c => c.String());
            AlterColumn("dbo.courses", "syllabus", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.courses", "syllabus", c => c.String(unicode: false, storeType: "text"));
            AlterColumn("dbo.courses", "description", c => c.String(unicode: false, storeType: "text"));
        }
    }
}
