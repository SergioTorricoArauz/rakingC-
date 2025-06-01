namespace RankingCyY.Data
{
    using Microsoft.EntityFrameworkCore;
    using RankingCyY.Models;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Temporadas> Temporadas { get; set; }
    }
}
