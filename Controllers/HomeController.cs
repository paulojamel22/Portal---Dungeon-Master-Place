using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;

namespace PortalDMPlace.Controllers
{
    public class HomeController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Buscamos apenas as campanhas. 
            // O dinamismo de cores e imagens será resolvido pelo Helper na View.
            var campanhas = await _context.Campanhas
                .AsNoTracking()
                .ToListAsync();

            if (campanhas == null || campanhas.Count == 0)
            {
                TempData["Info"] = "Nenhuma crônica foi iniciada ainda.";
            }

            return View(campanhas);
        }
    }
}
