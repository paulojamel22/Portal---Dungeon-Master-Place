using Microsoft.AspNetCore.Mvc;
using PortalDMPlace.Models;

namespace PortalDMPlace.Controllers
{
    public class HomeController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public IActionResult Index()
        {
            var campanhas = _context.Campanhas.ToList();

            if (campanhas == null || campanhas.Count == 0)
            {
                TempData["Info"] = "Nenhuma campanha encontrada. Crie uma nova campanha.";
            }
            return View(campanhas);
        }
    }
}
