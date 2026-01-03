using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PortalDMPlace.Controllers
{
    public partial class AdminController(DataContext context, IHttpClientFactory httpClientFactory) : Controller
    {
        private readonly DataContext _context = context;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        [GeneratedRegex(@"<\/?(div|article|span|cite)[^>]*>", RegexOptions.IgnoreCase)]
        private static partial Regex TagsIndesejadasRegex();

        [HttpGet]
        public IActionResult Index()
        {
            var noticias = _context.Noticias
                .OrderByDescending(n => n.DataPublicacao)
                .ToList();

            return View(noticias);
        }

        [HttpGet]
        public IActionResult Criar()
        {
            var name = _context.Accounts.FirstOrDefault()?.Name;

            // Opcional: pode enviar categorias pré-definidas pro select se quiser
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            ViewBag.Name = name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([BindRequired] Noticia noticia, IFormFile? ImagemFile)
        {
            // Removemos validação de conteúdo se você usa editor Rich Text
            ModelState.Remove("Conteudo");

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Verifique os campos obrigatórios.";
                return View(noticia);
            }

            try
            {
                noticia.DataPublicacao = DateTime.Now;

                if (ImagemFile != null && ImagemFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImagemFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImagemFile.CopyToAsync(stream);
                    }
                    noticia.ImagemUrl = "/imagens/" + fileName;
                }

                _context.Noticias.Add(noticia);
                await _context.SaveChangesAsync();

                // 🚀 Chamada Assíncrona para o Discord
                await EnviarNoticiaDiscord(noticia);

                TempData["Sucesso"] = "Notícia publicada e enviada ao Discord!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erro: " + ex.Message;
                return View(noticia);
            }
        }

        // GET: /Noticias/Detalhes/5
        public async Task<IActionResult> Detalhes(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Admin");
            }

            var noticia = await _context.Noticias.FirstOrDefaultAsync(n => n.Id == id);

            if (noticia == null)
            {
                TempData["Erro"] = "Notícia não encontrada.";
                return RedirectToAction("Index", "Admin");
            }

            return View(noticia);
        }

        public IActionResult Excluir(int id)
        {
            var noticia = _context.Noticias.Find(id);
            if (noticia == null) return NotFound();

            _context.Noticias.Remove(noticia);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            var noticia = _context.Noticias.Find(id);
            if (noticia == null)
                return NotFound();

            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            return View(noticia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Noticia noticia, IFormFile? ImagemFile)
        {
            // ISENÇÃO DE VALIDAÇÃO: Removemos o erro de ImagemUrl do ModelState
            ModelState.Remove("ImagemUrl");
            
            // Se você não enviar a Campanha completa (objeto), remova também:
            ModelState.Remove("Campanha");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
                Console.WriteLine("ModelState Errors: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(noticia);
            }
            else
            {
                Console.WriteLine("ModelState is valid.");
            }

            var noticiaExistente = _context.Noticias.Find(noticia.Id);
            if (noticiaExistente == null)
                return NotFound();

            try
            {
                noticiaExistente.Titulo = noticia.Titulo;
                noticiaExistente.Conteudo = noticia.Conteudo;
                noticiaExistente.Campanha = noticia.Campanha;
                noticiaExistente.Categoria = noticia.Categoria;
                noticiaExistente.Autor = noticia.Autor;

                if (ImagemFile != null && ImagemFile.Length > 0)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens");
                    if (!Directory.Exists(uploadsPath))
                        Directory.CreateDirectory(uploadsPath);

                    var fileName = Guid.NewGuid() + Path.GetExtension(ImagemFile.FileName);
                    var filePath = Path.Combine(uploadsPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        ImagemFile.CopyTo(stream);

                    noticiaExistente.ImagemUrl = "/imagens/" + fileName;
                }
                else
                {
                    // Mantém a imagem existente
                    noticiaExistente.ImagemUrl = noticia.ImagemUrl;
                }

                _context.SaveChanges();
                TempData["Sucesso"] = "Notícia atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erro ao atualizar: " + ex.Message;
                TempData["Error"] = "Erro ao atualizar: " + ex.Message;
                Console.WriteLine("Exception: " + ex.Message);
                ViewBag.Categorias = new List<string> { "Avisos", "Atualizações", "Eventos", "Lore" };
                return View(noticia);
            }
        }

        // --- Login ---
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.Accounts.FirstOrDefault(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
            {
                ViewBag.Erro = "Usuário ou senha inválidos.";
                return View();
            }

            // 1. Criar as "Alegações" (Claims) do usuário
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name), // Este aqui é o que aparece no @User.Identity.Name
                new(ClaimTypes.NameIdentifier, user.Username),
                new("CampanhaId", user.CampanhaId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Efetuar o Login Real no ASP.NET
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                new AuthenticationProperties { IsPersistent = true });

            // Mantemos a sessão para compatibilidade com seus códigos anteriores
            HttpContext.Session.SetInt32("Campanha", user.CampanhaId);
            HttpContext.Session.SetString("Usuario", user.Username);

            return RedirectToAction("Index", "Admin");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // --- Registro (opcional, só GM deve criar contas) ---
        [HttpGet]
        public IActionResult Registrar() => View();

        [HttpPost]
        public IActionResult Registrar(Account model)
        {
            if (_context.Accounts.Any(u => u.Username == model.Username))
            {
                ViewBag.Erro = "Usuário já existe.";
                return View();
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            model.HashPassword = hash;
            model.Password = string.Empty;

            _context.Accounts.Add(model);
            _context.SaveChanges();

            ViewBag.Sucesso = "Usuário criado com sucesso!";
            return RedirectToAction("Login");
        }

        private async Task EnviarNoticiaDiscord(Noticia noticia)
        {
            try
            {
                // 🔍 BUSCA DINÂMICA: Pega o webhook da campanha específica no banco
                var settings = await _context.Settings
                    .FirstOrDefaultAsync(s => s.CampanhaId == noticia.CampanhaId);

                if (settings == null || string.IsNullOrEmpty(settings.DiscordWebhookUrl))
                    return; // Se não tem webhook configurado no servidor, ignora silenciosamente

                var client = _httpClientFactory.CreateClient();
                var converter = new Html2Markdown.Converter();
                string conteudoMarkdown = converter.Convert(noticia.Conteudo);

                // Limpeza de texto para o Embed
                string conteudoLimpo = TagsIndesejadasRegex().Replace(conteudoMarkdown, " ").Trim();
                if (conteudoLimpo.Length > 500) conteudoLimpo = string.Concat(conteudoLimpo.AsSpan(0, 500), "...");

                var payload = new
                {
                    username = "Aetheria News",
                    content = "@everyone",
                    embeds = new[]
                    {
                        new
                        {
                            title = noticia.Titulo,
                            description = conteudoLimpo,
                            color = noticia.CampanhaId == 1 ? 0xFFD700 : 0x8B0000, 
                            url = $"https://portal.dmplace.com.br/Aetheria/Detalhes/{noticia.Id}",
                            image = string.IsNullOrEmpty(noticia.ImagemUrl) ? null : new { url = "https://portal.dmplace.com.br/img/default.jpg" },
                            footer = new { text = $"{noticia.Categoria} • Por {noticia.Autor}" }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PostAsync(settings.DiscordWebhookUrl, content);
            }
            catch (Exception ex)
            {
                // Logar o erro sem derrubar o site
                Console.WriteLine($"[Discord Error] {ex.Message}");
            }
        }

        //=====================================================================================
        // Campanhas
        //=====================================================================================

        [HttpGet]
        public IActionResult Campanhas()
        {
            var campanhas = _context.Campanhas.ToList();

            if(campanhas == null || campanhas.Count == 0)
            {
                TempData["Info"] = "Nenhuma campanha encontrada. Crie uma nova campanha.";
            }

            return View("Campanhas/Index",campanhas);
        }

        [HttpGet]
        public IActionResult CriarCampanha()
        {
            return View("Campanhas/Adicionar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CriarCampanha(Campanha campanha)
        {
            if (!ModelState.IsValid)
                return View("Campanhas/Adicionar", campanha);

            _context.Campanhas.Add(campanha);
            _context.SaveChanges();
            TempData["Sucesso"] = "Campanha criada com sucesso!";
            return RedirectToAction(nameof(Campanhas));
        }

        [HttpGet]
        public IActionResult EditarCampanha(int id)
        {
            var campanha = _context.Campanhas.Find(id);
            if (campanha == null)
                return NotFound();
            return View("Campanhas/Editar", campanha);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarCampanha(Campanha campanha)
        {
            if (!ModelState.IsValid)
                return View("Campanhas/Editar", campanha);

            _context.Campanhas.Update(campanha);
            _context.SaveChanges();
            TempData["Sucesso"] = "Campanha atualizada!";
            return RedirectToAction(nameof(Campanhas));
        }

        // DELETAR (GET)
        [HttpGet]
        public IActionResult DeletarCampanha(int id)
        {
            var campanha = _context.Campanhas.Find(id);
            if (campanha == null) return NotFound();

            return View("Campanhas/Deletar", campanha);
        }

        // DELETAR (POST)
        [HttpPost, ActionName("DeletarCampanha")]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmarDeletarCampanha(int id)
        {
            var campanha = _context.Campanhas.Find(id);
            if (campanha == null) return NotFound();

            _context.Campanhas.Remove(campanha);
            _context.SaveChanges();
            TempData["Sucesso"] = "Campanha excluída!";
            return RedirectToAction(nameof(Campanhas));
        }

        //=====================================================================================
        // Gerenciamento de Configurações (Settings) - NOVO
        //=====================================================================================

        [HttpGet]
        public async Task<IActionResult> Configurar()
        {
            // Buscamos todas as campanhas e trazemos junto o objeto de Settings de cada uma
            var campanhas = await _context.Campanhas
                .Include(c => c.Settings)
                .ToListAsync();

            return View(campanhas);
        }

        [HttpPost]
        public async Task<IActionResult> Configurar(Settings config)
        {
            if (config.Id == 0) _context.Settings.Add(config);
            else _context.Settings.Update(config);

            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Configurações salvas!";
            return RedirectToAction(nameof(Campanhas));
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
            return RedirectToAction(nameof(Configurar));
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
    }
}
