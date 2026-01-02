using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using System.Numerics;

namespace PortalDMPlace.Controllers
{
    public class AetheriaController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            int NoticiasPorPagina = 6;

            var noticias = _context.Noticias
                .Where(n => n.Campanha == 1)
                .OrderByDescending(n => n.DataPublicacao);

            var TotalNoticia = await noticias.CountAsync();

            var GetPage = await noticias.Skip((page - 1) * NoticiasPorPagina).Take(NoticiasPorPagina).ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(TotalNoticia / (double)NoticiasPorPagina);
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };

            return View(GetPage);
        }

        [HttpGet]
        public async Task<IActionResult> Detalhes(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Aetheria");
            }

            var noticia = await _context.Noticias.FirstOrDefaultAsync(n => n.Id == id);

            if (noticia == null)
            {
                TempData["Erro"] = "Notícia não encontrada.";
                return RedirectToAction("Index", "Aetheria");
            }

            return View(noticia);
        }
    }
}