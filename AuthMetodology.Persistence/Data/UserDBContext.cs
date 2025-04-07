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
    }
}
