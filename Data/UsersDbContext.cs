using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) {}

    public DbSet<User> Users => Set<User>();

}