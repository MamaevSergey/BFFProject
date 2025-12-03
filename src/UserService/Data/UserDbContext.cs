using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        // Эта строчка говорит: "В базе должна быть таблица Users, которая хранит объекты User"
        public DbSet<User> Users { get; set; }
    }
}