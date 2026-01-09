using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using PortalDMPlace.Functions;
using Microsoft.AspNetCore.Authorization;
using PortalDMPlace.Models.ViewModels;

namespace PortalDMPlace.Controllers
{
    public class CampanhaController(DataContext context, AccountObject user) : Controller
    {
        private readonly DataContext _context = context;
        private readonly AccountObject _user = user;
        HelpersFunctions Helpers => new(_context);

        // --- ÁREA PÚBLICA (VISITANTES) ---

        [HttpGet("C/{slug}")]
        public async Task<IActionResult> Noticias(string slug, int page = 1, string? categoria = null)
        {
            var campanha = await _context.Campanhas
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.NomeSimples.ToLower() == slug.ToLower());

            if (campanha == null) return RedirectToAction("Index", "Home");

            var settings = await Helpers.GetSettingsAsync(campanha.Id);

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

            ViewBag.Settings = settings;
            ViewBag.Campanha = campanha;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalNoticias / (double)noticiasPorPagina);
            ViewBag.CategoriaAtual = categoria;
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };

            return View(noticias);
        }

        // --- DETALHES DA CRÔNICA (PÚBLICO) ---
        [HttpGet("C/{slug}/{id}")]
        public async Task<IActionResult> Detalhes(string slug, int id)
        {
            // Busca a notícia incluindo a campanha para garantir que pertence ao mundo certo
            var noticia = await _context.Noticias
                .Include(n => n.Campanha)
                .FirstOrDefaultAsync(n => n.Id == id && n.Campanha.NomeSimples.ToLower() == slug.ToLower());

            if (noticia == null) return NotFound();

            // Busca as configurações visuais do mundo
            var settings = await Helpers.GetSettingsAsync(noticia.CampanhaId);

            ViewBag.Settings = settings;
            ViewBag.Campanha = noticia.Campanha;

            // Retorna a view Detalhes.cshtml que revisamos anteriormente
            return View(noticia);
        }

        // --- ÁREA ADMINISTRATIVA (MESTRES/ADMINS) ---

        [Authorize]
        [HttpGet("C/Campanhas")] // Central de Mundos
        public async Task<IActionResult> Index()
        {
            IQueryable<Campanha> query = _context.Campanhas.AsNoTracking();

            if (!_user.IsAtLeastAdmin) 
                query = query.Where(c => c.CriadorId == _user.Id);

            var campanhas = await query.ToListAsync();
            return View("~/Views/Admin/Campanhas/Index.cshtml", campanhas);
        }

        [Authorize]
        [HttpGet("Admin/Campanhas/Editar/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var campanha = await _context.Campanhas.FindAsync(id);
            if (campanha == null) return NotFound();

            // SOBERANIA: Validar se é o dono
            if (campanha.CriadorId != _user.Id && !_user.IsAtLeastAdmin)
                return Forbid();

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == id) 
                        ?? new Settings { CampanhaId = id };

            var viewModel = new CampanhaSettingsViewModel
            {
                CampanhaId = campanha.Id,
                CriadorId = campanha.CriadorId,
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
                VttUrl = settings.VttUrl
            };

            return View("~/Views/Admin/Campanhas/Editar.cshtml", viewModel);
        }

        [Authorize]
        [HttpPost("Admin/Campanhas/Editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CampanhaSettingsViewModel model, IFormFile? bannerFile, IFormFile? thumbFile)
        {
            if (id != model.CampanhaId) return BadRequest();

            // SOBERANIA NO POST: Busca a campanha do banco para validar o dono real
            var dbCampanha = await _context.Campanhas.FindAsync(id);
            if (dbCampanha == null) return NotFound();
            
            if (dbCampanha.CriadorId != _user.Id && !_user.IsAtLeastAdmin)
                return Forbid();

            if (!ModelState.IsValid) return View("~/Views/Admin/Campanhas/Editar.cshtml", model);

            try
            {
                var dbSettings = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == id);
                if (dbSettings == null)
                {
                    dbSettings = new Settings { CampanhaId = id };
                    _context.Settings.Add(dbSettings);
                }

                dbCampanha.Name = model.Name;
                dbCampanha.NomeSimples = model.NomeSimples;
                dbCampanha.Description = model.Description ?? "";

                // --- UPLOAD REFINADO ---
                string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/campanhas");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                if (bannerFile?.Length > 0)
                    dbSettings.BannerUrl = await HandleFileUpload(bannerFile, "banner", id, dbSettings.BannerUrl);

                if (thumbFile?.Length > 0)
                    dbSettings.CardThumbnailUrl = await HandleFileUpload(thumbFile, "thumb", id, dbSettings.CardThumbnailUrl);

                dbSettings.TemaCorPrimaria = model.TemaCorPrimaria ?? "#ffc107";
                dbSettings.TemaCorSecundaria = model.TemaCorSecundaria ?? "#6c757d";
                dbSettings.ChamadaCard = model.ChamadaCard ?? "Bem-vindo ao portal!";
                dbSettings.FonteFamilia = model.FonteFamilia ?? "'Segoe UI', sans-serif";
                dbSettings.DiscordWebhookUrl = model.DiscordWebhookUrl ?? "";
                dbSettings.VttUrl = model.VttUrl ?? "";

                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "A essência do mundo foi moldada!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erro ao salvar a realidade: " + ex.Message);
                return View("~/Views/Admin/Campanhas/Editar.cshtml", model);
            }
        }

        // Helper Privado para Processar Uploads e Limpar Antigos
        private static async Task<string> HandleFileUpload(IFormFile file, string prefix, int id, string? currentUrl)
        {
            string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/campanhas");
            
            // Apaga o antigo se existir e não for padrão
            if (!string.IsNullOrEmpty(currentUrl) && !currentUrl.Contains("default.png"))
            {
                string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", currentUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"{prefix}_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return $"/img/campanhas/{fileName}";
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
                model.CriadorId = _user.Id; // Define o Criador como o usuário atual

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

            // Se não for o dono e não for Admin, acesso negado
            if (campanha.CriadorId != _user.Id && !_user.IsAtLeastAdmin)
            {
                return Forbid(); // Ou redireciona com erro
            }

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