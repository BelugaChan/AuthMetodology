using AuthMetodology.Logic.Entities;
using Microsoft.EntityFrameworkCore;
namespace AuthMetodology.Persistence.Data
{
    public class UserDBContext : DbContext
    {
        public UserDBContext(DbContextOptions<UserDBContext> options)
            : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
    }
}
