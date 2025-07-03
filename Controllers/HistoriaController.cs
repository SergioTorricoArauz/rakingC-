using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;
using RankingCyY.Hubs;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HistoriaController(AppDbContext context, IWebHostEnvironment environment, IHubContext<HistoriaHub> hubContext) : ControllerBase
    {
        private readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long _tamañoMaximo = 5 * 1024 * 1024; // 5MB

        // Crea una nueva historia
        [HttpPost("crear")]
        public async Task<IActionResult> CrearHistoria([FromForm] HistoriaPostDto dto)
        {
            if (dto.Imagenes == null || !dto.Imagenes.Any())
                return BadRequest("Debe subir al menos una imagen");

            if (dto.Imagenes.Count > 10)
                return BadRequest("Máximo 10 imágenes por historia");

            var cliente = await context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return BadRequest("Cliente no encontrado");

            try
            {
                var historia = new Historia
                {
                    ClienteId = dto.ClienteId,
                    Descripcion = dto.Descripcion,
                    FechaCreacion = DateTime.UtcNow,
                    FechaExpiracion = DateTime.UtcNow.AddHours(dto.DuracionHoras),
                    PermiteComentarios = dto.PermiteComentarios,
                    EstaActiva = true
                };

                context.Historias.Add(historia);
                await context.SaveChangesAsync();

                var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "historys");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                for (int i = 0; i < dto.Imagenes.Count; i++)
                {
                    var imagen = dto.Imagenes[i];
                    
                    var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
                    if (!_extensionesPermitidas.Contains(extension))
                        continue;

                    if (imagen.Length > _tamañoMaximo)
                        continue;

                    var nombreArchivo = $"{historia.Id}_{i}_{Guid.NewGuid()}{extension}";
                    var rutaCompleta = Path.Combine(uploadsPath, nombreArchivo);

                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }

                    var historiaImagen = new HistoriaImagen
                    {
                        HistoriaId = historia.Id,
                        NombreArchivo = nombreArchivo,
                        RutaArchivo = $"/uploads/historys/{nombreArchivo}",
                        Orden = i
                    };

                    context.HistoriaImagenes.Add(historiaImagen);
                }

                await context.SaveChangesAsync();

                // Notificar a todos los clientes conectados sobre la nueva historia
                var historiaResponse = new HistoriaResponseDto
                {
                    Id = historia.Id,
                    ClienteId = historia.ClienteId,
                    NombreCliente = cliente.Nombre,
                    Descripcion = historia.Descripcion,
                    FechaCreacion = historia.FechaCreacion,
                    FechaExpiracion = historia.FechaExpiracion,
                    EstaActiva = historia.EstaActiva,
                    PermiteComentarios = historia.PermiteComentarios,
                    PuedeComentarAun = historia.PermiteComentarios && historia.FechaExpiracion > DateTime.UtcNow,
                    Imagenes = await context.HistoriaImagenes
                        .Where(i => i.HistoriaId == historia.Id)
                        .OrderBy(i => i.Orden)
                        .Select(i => new HistoriaImagenDto
                        {
                            Id = i.Id,
                            NombreArchivo = i.NombreArchivo,
                            Url = $"{Request.Scheme}://{Request.Host}{i.RutaArchivo}",
                            Orden = i.Orden
                        }).ToListAsync(),
                    Comentarios = []
                };

                await hubContext.Clients.All.SendAsync("NuevaHistoria", historiaResponse);

                return Ok(new { success = true, historiaId = historia.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al crear historia: {ex.Message}");
            }
        }

        // Obtiene todas las historias activas
        [HttpGet("activas")]
        public async Task<IActionResult> ObtenerHistoriasActivas([FromQuery] int? clienteActualId = null)
        {
            var ahora = DateTime.UtcNow;
            
            var historias = await context.Historias
                .Where(h => h.EstaActiva && h.FechaExpiracion > ahora)
                .Include(h => h.Cliente)
                .Include(h => h.Imagenes)
                .Include(h => h.Comentarios)
                    .ThenInclude(c => c.Cliente)
                .Include(h => h.Comentarios)
                    .ThenInclude(c => c.ComentarioLikes)
                .OrderByDescending(h => h.FechaCreacion)
                .Select(h => new HistoriaResponseDto
                {
                    Id = h.Id,
                    ClienteId = h.ClienteId,
                    NombreCliente = h.Cliente.Nombre,
                    Descripcion = h.Descripcion,
                    FechaCreacion = h.FechaCreacion,
                    FechaExpiracion = h.FechaExpiracion,
                    EstaActiva = h.EstaActiva,
                    PermiteComentarios = h.PermiteComentarios,
                    PuedeComentarAun = h.PermiteComentarios && h.FechaExpiracion > ahora,
                    Imagenes = h.Imagenes.OrderBy(i => i.Orden).Select(i => new HistoriaImagenDto
                    {
                        Id = i.Id,
                        NombreArchivo = i.NombreArchivo,
                        Url = $"{Request.Scheme}://{Request.Host}{i.RutaArchivo}",
                        Orden = i.Orden
                    }).ToList(),
                    Comentarios = h.Comentarios.OrderBy(c => c.FechaComentario).Select(c => new HistoriaComentarioDto
                    {
                        Id = c.Id,
                        ClienteId = c.ClienteId,
                        NombreCliente = c.Cliente.Nombre,
                        Comentario = c.Comentario,
                        FechaComentario = c.FechaComentario,
                        Likes = c.Likes,
                        YaLeDiLike = clienteActualId.HasValue && c.ComentarioLikes.Any(l => l.ClienteId == clienteActualId.Value)
                    }).ToList()
                })
                .ToListAsync();

            return Ok(historias);
        }

        // Obtiene una historia por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerHistoria(int id, [FromQuery] int? clienteActualId = null)
        {
            var historia = await context.Historias
                .Where(h => h.Id == id)
                .Include(h => h.Cliente)
                .Include(h => h.Imagenes)
                .Include(h => h.Comentarios)
                    .ThenInclude(c => c.Cliente)
                .Include(h => h.Comentarios)
                    .ThenInclude(c => c.ComentarioLikes)
                .FirstOrDefaultAsync();

            if (historia == null)
                return NotFound("Historia no encontrada");

            var ahora = DateTime.UtcNow;
            var response = new HistoriaResponseDto
            {
                Id = historia.Id,
                ClienteId = historia.ClienteId,
                NombreCliente = historia.Cliente.Nombre,
                Descripcion = historia.Descripcion,
                FechaCreacion = historia.FechaCreacion,
                FechaExpiracion = historia.FechaExpiracion,
                EstaActiva = historia.EstaActiva,
                PermiteComentarios = historia.PermiteComentarios,
                PuedeComentarAun = historia.PermiteComentarios && historia.FechaExpiracion > ahora,
                Imagenes = historia.Imagenes.OrderBy(i => i.Orden).Select(i => new HistoriaImagenDto
                {
                    Id = i.Id,
                    NombreArchivo = i.NombreArchivo,
                    Url = $"{Request.Scheme}://{Request.Host}{i.RutaArchivo}",
                    Orden = i.Orden
                }).ToList(),
                Comentarios = historia.Comentarios.OrderBy(c => c.FechaComentario).Select(c => new HistoriaComentarioDto
                {
                    Id = c.Id,
                    ClienteId = c.ClienteId,
                    NombreCliente = c.Cliente.Nombre,
                    Comentario = c.Comentario,
                    FechaComentario = c.FechaComentario,
                    Likes = c.Likes,
                    YaLeDiLike = clienteActualId.HasValue && c.ComentarioLikes.Any(l => l.ClienteId == clienteActualId.Value)
                }).ToList()
            };

            return Ok(response);
        }

        // Comentar una historia
        [HttpPost("comentar")]
        public async Task<IActionResult> ComentarHistoria([FromBody] ComentarioPostDto dto)
        {
            var historia = await context.Historias.FindAsync(dto.HistoriaId);
            if (historia == null)
                return NotFound("Historia no encontrada");

            var ahora = DateTime.UtcNow;
            if (!historia.PermiteComentarios || historia.FechaExpiracion <= ahora)
                return BadRequest("No se pueden agregar comentarios a esta historia");

            var cliente = await context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return BadRequest("Cliente no encontrado");

            var comentario = new HistoriaComentario
            {
                HistoriaId = dto.HistoriaId,
                ClienteId = dto.ClienteId,
                Comentario = dto.Comentario,
                FechaComentario = DateTime.UtcNow
            };

            context.HistoriaComentarios.Add(comentario);
            await context.SaveChangesAsync();

            // Crear el DTO del comentario para enviar en tiempo real
            var comentarioDto = new HistoriaComentarioDto
            {
                Id = comentario.Id,
                ClienteId = comentario.ClienteId,
                NombreCliente = cliente.Nombre,
                Comentario = comentario.Comentario,
                FechaComentario = comentario.FechaComentario,
                Likes = 0,
                YaLeDiLike = false
            };

            // Notificar a todos los clientes conectados al grupo de esta historia
            await hubContext.Clients.Group($"Historia_{dto.HistoriaId}")
                .SendAsync("NuevoComentario", dto.HistoriaId, comentarioDto);

            return Ok(new { success = true, comentarioId = comentario.Id, comentario = comentarioDto });
        }

        // Dar like a un comentario
        [HttpPost("like-comentario/{comentarioId}")]
        public async Task<IActionResult> DarLikeComentario(int comentarioId, [FromQuery] int clienteId)
        {
            var comentario = await context.HistoriaComentarios
                .Include(c => c.ComentarioLikes)
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.Id == comentarioId);

            if (comentario == null)
                return NotFound("Comentario no encontrado");

            var yaLeDioLike = comentario.ComentarioLikes.Any(l => l.ClienteId == clienteId);

            if (yaLeDioLike)
            {
                var like = comentario.ComentarioLikes.First(l => l.ClienteId == clienteId);
                context.ComentarioLikes.Remove(like);
                comentario.Likes--;
            }
            else
            {
                var like = new ComentarioLike
                {
                    ComentarioId = comentarioId,
                    ClienteId = clienteId
                };
                context.ComentarioLikes.Add(like);
                comentario.Likes++;
            }

            await context.SaveChangesAsync();
            
            var comentarioActualizado = await context.HistoriaComentarios
                .Include(c => c.Cliente)
                .Include(c => c.ComentarioLikes)
                .FirstOrDefaultAsync(c => c.Id == comentarioId);

            var response = new HistoriaComentarioDto
            {
                Id = comentarioActualizado!.Id,
                ClienteId = comentarioActualizado.ClienteId,
                NombreCliente = comentarioActualizado.Cliente.Nombre,
                Comentario = comentarioActualizado.Comentario,
                FechaComentario = comentarioActualizado.FechaComentario,
                Likes = comentarioActualizado.Likes,
                YaLeDiLike = comentarioActualizado.ComentarioLikes.Any(l => l.ClienteId == clienteId)
            };

            // Notificar a todos los clientes conectados al grupo de esta historia sobre el cambio de likes
            await hubContext.Clients.Group($"Historia_{comentario.HistoriaId}")
                .SendAsync("ComentarioLikeActualizado", comentarioId, response.Likes, !yaLeDioLike);
            
            return Ok(new { 
                success = true, 
                comentario = response
            });
        }

        // Obtener un comentario específico
        [HttpGet("comentario/{comentarioId}")]
        public async Task<IActionResult> ObtenerComentario(int comentarioId, [FromQuery] int? clienteActualId = null)
        {
            var comentario = await context.HistoriaComentarios
                .Include(c => c.Cliente)
                .Include(c => c.ComentarioLikes)
                .FirstOrDefaultAsync(c => c.Id == comentarioId);

            if (comentario == null)
                return NotFound("Comentario no encontrado");

            var response = new HistoriaComentarioDto
            {
                Id = comentario.Id,
                ClienteId = comentario.ClienteId,
                NombreCliente = comentario.Cliente.Nombre,
                Comentario = comentario.Comentario,
                FechaComentario = comentario.FechaComentario,
                Likes = comentario.Likes,
                YaLeDiLike = clienteActualId.HasValue && comentario.ComentarioLikes.Any(l => l.ClienteId == clienteActualId.Value)
            };

            return Ok(response);
        }
    }
}
