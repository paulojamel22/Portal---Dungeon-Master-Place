using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using PortalDMPlace.Functions;
using Microsoft.AspNetCore.Authorization;
using PortalDMPlace.Models.ViewModels;

namespace PortalDMPlace.Controllers
{
    public class CampanhaController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;
        HelpersFunctions Helpers => new(_context);

        // Rota principal da Campanha (ex: portal.com.br/Aetheria)
        [HttpGet("C/{slug}")] //C/{prefixo}/Noticias
        public async Task<IActionResult> Noticias(string slug, int page = 1, string? categoria = null)
        {
            // Lista de nomes que o slug NUNCA pode assumir
            string[] reserved = ["admin", "home", "login", "css", "js", "img"];
            if (reserved.Contains(slug.ToLower()))
            {
                // Se cair aqui, ele ignora e o ASP.NET tenta achar o controller físico
                return NotFound(); 
            }

            // 1. Identifica a Campanha pelo NomeSimples (Slug)
            var campanha = await _context.Campanhas
                .FirstOrDefaultAsync(c => c.NomeSimples.ToLower() == slug.ToLower());

            if (campanha == null) return RedirectToAction("Index", "Home");

            // 2. Busca as Configurações Visuais (Settings) usando seu Helper
            var settings = await Helpers.GetSettingsAsync(campanha.Id);

            // 3. Lógica de Notícias (unificada dos seus dois controllers)
            int noticiasPorPagina = 6;
            var query = _context.Noticias
                .Where(n => n.CampanhaId == campanha.Id)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(categoria))
                query = query.Where(n => n.Categoria == categoria);

            var totalNoticias = await query.CountAsync();
            var noticias = await query
                .OrderByDescending(n => n.DataPublicacao)
                .Skip((page - 1) * noticiasPorPagina)
                .Take(noticiasPorPagina)
                .ToListAsync();

            // 4. Metadados para a View Dinâmica
            ViewBag.Settings = settings;
            ViewBag.Campanha = campanha;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalNoticias / (double)noticiasPorPagina);
            ViewBag.CategoriaAtual = categoria;
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };

            // Retornamos a View genérica que servirá para todas as campanhas
            return View(noticias);
        }

        // Detalhes da Notícia (ex: portal.com.br/Aetheria/Detalhes/15)
        [HttpGet("{slug}/Detalhes/{id}")]
        public async Task<IActionResult> Detalhes(string slug, int id)
        {
            var campanha = await _context.Campanhas
                .FirstOrDefaultAsync(c => c.NomeSimples.ToLower() == slug.ToLower());

            if (campanha == null || id <= 0) return NotFound();

            var noticia = await _context.Noticias
                .Include(n => n.Campanha)
                .FirstOrDefaultAsync(n => n.Id == id && n.CampanhaId == campanha.Id);

            if (noticia == null) return RedirectToAction(nameof(Index), new { slug });

            // Carregamos os settings também para os detalhes terem as cores certas
            ViewBag.Settings = await Helpers.GetSettingsAsync(campanha.Id);
            ViewBag.Campanha = campanha;

            return View(noticia);
        }

        // GET: Admin/Campanhas/Editar/5
        [Authorize]
        [HttpGet("Admin/Campanhas/Editar/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var campanha = await _context.Campanhas.FindAsync(id);
            if (campanha == null) return NotFound();

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == id) 
                        ?? new Settings { CampanhaId = id };

            // Montamos o ViewModel para a View Unificada
            var viewModel = new CampanhaSettingsViewModel
            {
                CampanhaId = campanha.Id,
                Name = campanha.Name,
                NomeSimples = campanha.NomeSimples,
                Description = campanha.Description,
                SettingsId = settings.Id,
                TemaCorPrimaria = settings.TemaCorPrimaria,
                TemaCorSecundaria = settings.TemaCorSecundaria,
                BannerUrl = settings.BannerUrl,
                CardThumbnailUrl = settings.CardThumbnailUrl,
                ChamadaCard = settings.ChamadaCard,
                FonteFamilia = settings.FonteFamilia,
                DiscordWebhookUrl = settings.DiscordWebhookUrl,
                FoundryUrl = settings.FoundryUrl
            };

            return View("~/Views/Admin/Campanhas/Editar.cshtml", viewModel);
        }

        // POST: Admin/Campanhas/Editar/5
        [Authorize]
        [HttpPost("Admin/Campanhas/Editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CampanhaSettingsViewModel model, IFormFile? bannerFile, IFormFile? thumbFile)
        {
            if (id != model.CampanhaId) return BadRequest();

            if (!ModelState.IsValid) 
                return View("~/Views/Admin/Campanhas/Editar.cshtml", model);

            try
            {
                var dbCampanha = await _context.Campanhas.FindAsync(id);
                var dbSettings = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == id);

                if (dbCampanha == null) return NotFound();

                // 1. Atualiza Dados Básicos da Campanha
                dbCampanha.Name = model.Name;
                dbCampanha.NomeSimples = model.NomeSimples;
                dbCampanha.Description = model.Description ?? "";

                // 2. Garante que Settings existam
                if (dbSettings == null)
                {
                    dbSettings = new Settings { CampanhaId = id };
                    _context.Settings.Add(dbSettings);
                }

                // 3. Processamento de Imagens (Seu código de upload aprimorado)
                string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/campanhas");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                if (bannerFile != null && bannerFile.Length > 0)
                {
                    string fileName = $"banner_{id}_{Guid.NewGuid()}{Path.GetExtension(bannerFile.FileName)}";
                    using (var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create))
                        await bannerFile.CopyToAsync(stream);
                    dbSettings.BannerUrl = $"/img/campanhas/{fileName}";
                }

                if (thumbFile != null && thumbFile.Length > 0)
                {
                    string fileName = $"thumb_{id}_{Guid.NewGuid()}{Path.GetExtension(thumbFile.FileName)}";
                    using (var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create))
                        await thumbFile.CopyToAsync(stream);
                    dbSettings.CardThumbnailUrl = $"/img/campanhas/{fileName}";
                }

                // 4. Atualiza Identidade e Integrações
                dbSettings.TemaCorPrimaria = model.TemaCorPrimaria ?? "#ffc107";
                dbSettings.TemaCorSecundaria = model.TemaCorSecundaria ?? "#6c757d";
                dbSettings.ChamadaCard = model.ChamadaCard ?? "Bem-vindo ao portal da campanha!";
                dbSettings.FonteFamilia = model.FonteFamilia ?? "'Segoe UI', sans-serif";
                dbSettings.DiscordWebhookUrl = model.DiscordWebhookUrl ?? "";
                dbSettings.FoundryUrl = model.FoundryUrl ?? "";

                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "A essência do mundo foi moldada com sucesso!";
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erro ao salvar a realidade: " + ex.Message);
                return View("~/Views/Admin/Campanhas/Editar.cshtml", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SalvarWebhook(int id, string webhookUrl)
        {
            // Procura se já existe uma configuração para esta campanha
            var config = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == id);

            if (config == null)
            {
                // Se não existir, cria uma nova
                config = new Settings 
                { 
                    CampanhaId = id, 
                    DiscordWebhookUrl = webhookUrl 
                };
                _context.Settings.Add(config);
            }
            else
            {
                // Se já existir, apenas atualiza a URL
                config.DiscordWebhookUrl = webhookUrl;
                _context.Settings.Update(config);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Webhook da campanha atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TestarWebhook(int id)
        {
            var config = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == id);
            
            if (config == null || string.IsNullOrEmpty(config.DiscordWebhookUrl))
            {
                return Json(new { success = false, message = "Webhook não configurado!" });
            }

            using var client = new HttpClient();
            var payload = new { content = "🔔 **Teste de Conexão:** O Portal DM Place está conectado com sucesso ao seu reino!" };
            
            var response = await client.PostAsJsonAsync(config.DiscordWebhookUrl, payload);

            if (response.IsSuccessStatusCode)
                return Json(new { success = true, message = "Sinal enviado ao Discord!" });
            
            return Json(new { success = false, message = "Erro ao enviar sinal. Verifique a URL." });
        }

        [HttpGet("C/Campanhas")]
        public IActionResult Index()
        {
            var campanhas = _context.Campanhas.ToList();

            if (campanhas == null || campanhas.Count == 0)
            {
                TempData["Info"] = "Nenhuma campanha encontrada. Crie uma nova campanha.";
            }

            return View("~/Views/Admin/Campanhas/Index.cshtml", campanhas);
        }

        [HttpGet("Admin/Campanhas/Criar")]
        public IActionResult Criar()
        {
            return View("~/Views/Admin/Campanhas/Criar.cshtml");
        }

        [HttpPost("Admin/Campanhas/Criar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Campanha model)
        {
            if (ModelState.IsValid)
            {
                _context.Campanhas.Add(model);
                await _context.SaveChangesAsync();
                
                // Criar um registro básico de Settings para não dar erro na edição
                var defaultSettings = new Settings { CampanhaId = model.Id, FonteFamilia = "'Segoe UI', sans-serif" };
                _context.Settings.Add(defaultSettings);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Mundo forjado! Agora, defina a identidade visual dele.";
                
                // Redireciona para a tela de edição/configuração unificada que acabamos de criar
                return RedirectToAction("Editar", new { id = model.Id });
            }
            return View(model);
        }

        [HttpGet("Admin/Campanhas/Deletar/{id}")]
        public IActionResult DeletarCampanha(int id)
        {
            var campanha = _context.Campanhas.Find(id);
            if (campanha == null) return NotFound();

            return View("~/Views/Admin/Campanhas/Deletar.cshtml", campanha);
        }

        [HttpPost("Admin/Campanhas/Deletar/{id}"), ActionName("DeletarCampanha")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmarDeletarCampanha(int id)
        {
            var campanha = _context.Campanhas.Find(id);
            if (campanha == null) return NotFound();

            _context.Campanhas.Remove(campanha);
            _context.SaveChanges();
            TempData["Sucesso"] = "Campanha excluída!";
            return RedirectToAction(nameof(Index));
        }
    }
}