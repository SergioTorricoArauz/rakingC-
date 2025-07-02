namespace RankingCyY.Data
{
    using Microsoft.EntityFrameworkCore;
    using RankingCyY.Models;

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Temporadas> Temporadas { get; set; }
        public DbSet<Insignias> Insignias { get; set; }
        public DbSet<ClienteInsignia> ClienteInsignias { get; set; }
        public DbSet<Puntajes> Puntajes { get; set; }
        public DbSet<Actividades> Actividades { get; set; }
        public DbSet<ClienteActividad> ClienteActividades { get; set; }
        public DbSet<Carrito> Carrito { get; set; }
        public DbSet<CarritoArticulos> CarritoArticulos { get; set; }
        public DbSet<Productos> Productos { get; set; }
        public DbSet<ProductosDescuento> ProductosDescuentos { get; set; }
        public DbSet<Historia> Historias { get; set; }
        public DbSet<HistoriaImagen> HistoriaImagenes { get; set; }
        public DbSet<HistoriaComentario> HistoriaComentarios { get; set; }
        public DbSet<ComentarioLike> ComentarioLikes { get; set; }

    }
}
