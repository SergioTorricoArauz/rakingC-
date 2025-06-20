using Microsoft.AspNetCore.Mvc;
using RankingCyY.Data;
using RankingCyY.Models;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ComprasController
    {
        private readonly AppDbContext _context;

        public ComprasController(AppDbContext context)
        {
            _context = context;
        }
    }
}
