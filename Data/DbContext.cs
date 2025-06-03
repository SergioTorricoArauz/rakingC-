namespace RankingCyY.Data
{
    using Microsoft.EntityFrameworkCore;
    using RankingCyY.Models;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Temporadas> Temporadas { get; set; }
        public DbSet<Insignias> Insignias { get; set; }
        public DbSet<ClienteInsignia> ClienteInsignias { get; set; }
        public DbSet<Puntajes> Puntajes { get; set; }
        public DbSet<Actividades> Actividades { get; set; }
        public DbSet<ClienteActividad> ClienteActividades { get; set; }
    }
}
