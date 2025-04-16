using AuthMetodology.Logic.Entities.v1;
using Microsoft.EntityFrameworkCore;
namespace AuthMetodology.Persistence.Data
{
    public class UserDBContext : DbContext
    {
        public UserDBContext(DbContextOptions<UserDBContext> options)
            : base(options)
        {
        }
        public DbSet<UserEntityV1> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //modelBuilder.Entity<UserEntityV1>()
            //.ToTable("auth", "authSchema");

            modelBuilder.Entity<UserEntityV1>()
                .Property(u => u.UserRole)
                .HasColumnName("userRole")
                .HasConversion<string>();
        }
    }
}
