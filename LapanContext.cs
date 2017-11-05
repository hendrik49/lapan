using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace LAPAN
{
    public class LapanContext : DbContext
    {
        public LapanContext()
        : base("LapanCtx")
        {
        Database.SetInitializer<LapanContext>(null);
        }
 
        public DbSet<TutupanLahan> TutupanLahans { get; set; }
        public DbSet<Rule> Rules { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.HasDefaultSchema("public");

            // user
            var user = modelBuilder.Entity<Rule>();
            user.ToTable("Rule");

            base.OnModelCreating(modelBuilder);
        }
    }
}
