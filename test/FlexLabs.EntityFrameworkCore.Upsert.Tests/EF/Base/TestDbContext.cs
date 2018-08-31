using System;
using Microsoft.EntityFrameworkCore;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF.Base
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Country>(c =>
            {
                c.HasKey(x => x.ID);
                c.Property(x => x.ID).ValueGeneratedOnAdd();
                c.HasIndex(x => x.ISO).IsUnique();
            });

            modelBuilder.Entity<PageVisit>(c =>
            {
                c.HasKey(x => x.ID);
                c.Property(x => x.ID).ValueGeneratedOnAdd();
                c.HasIndex(x => new { x.UserID, x.Date }).IsUnique();
            });

            modelBuilder.Entity<DashTable>(c =>
            {
                c.HasKey(x => x.ID);
                c.Property(x => x.ID).ValueGeneratedOnAdd();
                c.HasIndex(x => x.DataSet).IsUnique();
            });

            modelBuilder.Entity<SchemaTable>(c =>
            {
                c.HasKey(x => x.ID);
                c.Property(x => x.ID).ValueGeneratedOnAdd();
                c.HasIndex(x => x.Name).IsUnique();
            });
        }

        public DbSet<Country> Countries { get; set; }
        public DbSet<PageVisit> PageVisits { get; set; }
        public DbSet<DashTable> DashTable { get; set; }
        public DbSet<SchemaTable> SchemaTable { get; set; }

        public enum DbDriver { Postgres, MSSQL, MySQL, Sqlite }
        public static DbContextOptions<TestDbContext> Configure(string connectionString, DbDriver driver)
        {
            var options = new DbContextOptionsBuilder<TestDbContext>();
            switch (driver)
            {
                case DbDriver.Postgres:
                    options.UseNpgsql(connectionString);
                    break;
                case DbDriver.MSSQL:
                    options.UseSqlServer(connectionString);
                    break;
                case DbDriver.MySQL:
                    options.UseMySql(connectionString);
                    break;
                case DbDriver.Sqlite:
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException("Invalid database driver: " + driver);
            }

            options.EnableSensitiveDataLogging();
            return options.Options;
        }
    }
}
