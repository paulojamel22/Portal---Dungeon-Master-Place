using System.Net.Http.Json; // Essencial para PostAsJsonAsync
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Functions;
using PortalDMPlace.Models;

namespace PortalDMPlace.Controllers
{
    [Authorize]
    [Route("Admin/Noticias")]
    public partial class NoticiaController(DataContext context, IHttpClientFactory httpClientFactory, AccountObject user) : Controller
    {
        private readonly DataContext _context = context;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly AccountObject _user = user; // Injetado para saber quem é o autor
        private HelpersFunctions Functions => new(_context);

        [GeneratedRegex(@"<\/?(div|article|span|cite)[^>]*>", RegexOptions.IgnoreCase)]
        private static partial Regex TagsIndesejadasRegex();

        // --- LISTAGEM ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var query = _context.Noticias.Include(n => n.Campanha).AsQueryable();

            // SOBERANIA: Se não for Admin/Dev, vê apenas as suas notícias
            if (!_user.IsAtLeastAdmin)
            {
                query = query.Where(n => n.AccountId == _user.Id);
            }

            var noticias = await query.OrderByDescending(n => n.DataPublicacao).ToListAsync();
            return View("~/Views/Admin/Noticias/Index.cshtml", noticias);
        }

        // --- CRIAÇÃO ---
        [HttpGet("Criar")]
        public async Task<IActionResult> Criar()
        {
            // Busca apenas as campanhas que o mestre possui (ou todas se for Admin)
            IQueryable<Campanha> campanhasQuery = _context.Campanhas;
            if (!_user.IsAtLeastAdmin)
                campanhasQuery = campanhasQuery.Where(c => c.CriadorId == _user.Id);

            ViewBag.Campanhas = await campanhasQuery.ToListAsync();
            ViewBag.Name = _user.Name;
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            ViewBag.DefaultAutor = _user.Name; // Sugere o nome do mestre logado
            
            return View("~/Views/Admin/Noticias/Criar.cshtml");
        }

        [HttpPost("Criar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Noticia noticia, IFormFile? ImagemFile, bool enviarDiscord = false)
        {
            ModelState.Remove("Conteudo");
            ModelState.Remove("Campanha");
            ModelState.Remove("AutorAccount"); // Evita validação do objeto virtual

            if (ModelState.IsValid)
            {
                try
                {
                    // Define o Autor (Soberania)
                    noticia.AccountId = _user.Id;
                    noticia.DataPublicacao = DateTime.Now;

                    if (ImagemFile != null && ImagemFile.Length > 0)
                    {
                        var fileName = $"news_{Guid.NewGuid()}{Path.GetExtension(ImagemFile.FileName)}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/noticias", fileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await ImagemFile.CopyToAsync(stream);
                        
                        noticia.ImagemUrl = "/img/noticias/" + fileName;
                    }

                    _context.Noticias.Add(noticia);
                    await _context.SaveChangesAsync();

                    // 3. Lógica Condicional do Discord
                    if (enviarDiscord)
                    {
                        // Busca a campanha e o webhook
                        var settings = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == noticia.CampanhaId);
                        
                        if (settings != null && !string.IsNullOrEmpty(settings.DiscordWebhookUrl))
                        {
                            await EnviarNoticiaDiscord(noticia);
                        }
                    }

                    TempData["Sucesso"] = "Crônica publicada nos anais do tempo!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Erro ao forjar notícia: " + ex.Message;
                }
            }

            // Recarrega as campanhas permitidas em caso de erro
            IQueryable<Campanha> campanhasQuery = _context.Campanhas;
            if (!_user.IsAtLeastAdmin) campanhasQuery = campanhasQuery.Where(c => c.CriadorId == _user.Id);
            ViewBag.Campanhas = await campanhasQuery.ToListAsync();
            
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            return View("~/Views/Admin/Noticias/Criar.cshtml", noticia);
        }

        // --- EDIÇÃO ---
        [HttpGet("Editar/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia == null) return NotFound();

            // SOBERANIA: Bloqueia se tentar editar notícia de outro mestre
            if (noticia.AccountId != _user.Id && !_user.IsAtLeastAdmin)
                return Forbid();

            IQueryable<Campanha> campanhasQuery = _context.Campanhas;
            if (!_user.IsAtLeastAdmin) campanhasQuery = campanhasQuery.Where(c => c.CriadorId == _user.Id);

            ViewBag.Campanhas = await campanhasQuery.ToListAsync();
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            return View("~/Views/Admin/Noticias/Editar.cshtml", noticia);
        }

        [HttpPost("Editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Noticia noticia, IFormFile? ImagemFile)
        {
            var noticiaExistente = await _context.Noticias.FindAsync(id);
            if (noticiaExistente == null) return NotFound();

            // SOBERANIA NO POST
            if (noticiaExistente.AccountId != _user.Id && !_user.IsAtLeastAdmin)
                return Forbid();

            ModelState.Remove("ImagemUrl");
            ModelState.Remove("Campanha");
            ModelState.Remove("AutorAccount");

            if (ModelState.IsValid)
            {
                noticiaExistente.Titulo = noticia.Titulo;
                noticiaExistente.Conteudo = noticia.Conteudo;
                noticiaExistente.CampanhaId = noticia.CampanhaId;
                noticiaExistente.Categoria = noticia.Categoria;
                noticiaExistente.Autor = noticia.Autor;

                if (ImagemFile != null && ImagemFile.Length > 0)
                {
                    // Apaga a imagem antiga se não for a default
                    if (!noticiaExistente.ImagemUrl.Contains("default.jpg"))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", noticiaExistente.ImagemUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    var fileName = $"news_{Guid.NewGuid()}{Path.GetExtension(ImagemFile.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/noticias", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await ImagemFile.CopyToAsync(stream);
                    
                    noticiaExistente.ImagemUrl = "/img/noticias/" + fileName;
                }

                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Crônica reescrita com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Campanhas = await _context.Campanhas.ToListAsync();
            return View("~/Views/Admin/Noticias/Editar.cshtml", noticia);
        }

        [HttpGet("Detalhes/{id}")]
        public async Task<IActionResult> Detalhes(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            // Buscamos a notícia incluindo os dados da Campanha para o cabeçalho
            var noticia = await _context.Noticias
            .Include(n => n.Campanha)
            .FirstOrDefaultAsync(n => n.Id == id);

            if (noticia == null)
            {
                TempData["Erro"] = "A crônica se perdeu nas brumas do tempo (Não encontrada).";
                return RedirectToAction(nameof(Index));
            }

            // Definimos o título para a Topbar do LayoutAdmin
            ViewData["Title"] = "Visualizando Crônica";
            return View("~/Views/Admin/Noticias/Detalhes.cshtml", noticia);
        }

        // --- EXCLUSÃO ---
        [HttpPost("Excluir/{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia == null) return NotFound();

            // SOBERANIA: Só o dono ou Admin apaga
            if (noticia.AccountId != _user.Id && !_user.IsAtLeastAdmin)
                return Forbid();

            // Apaga a imagem associada
            if (!noticia.ImagemUrl.Contains("default.jpg"))
            {
                var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", noticia.ImagemUrl.TrimStart('/'));
                if (System.IO.File.Exists(imgPath)) System.IO.File.Delete(imgPath);
            }

            _context.Noticias.Remove(noticia);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Crônica apagada da história.";
            
            return RedirectToAction(nameof(Index));
        }

        // --- DISCORD WEBHOOK ---
        private async Task EnviarNoticiaDiscord(Noticia noticia)
        {
            // Busca o NomeSimples (slug) da campanha para gerar o link correto
            var campanha = await _context.Campanhas.FindAsync(noticia.CampanhaId);
            if (campanha == null) return;

            try
            {
                var settings = await _context.Settings
                    .FirstOrDefaultAsync(s => s.CampanhaId == noticia.CampanhaId);

                if (settings == null || string.IsNullOrEmpty(settings.DiscordWebhookUrl))
                    return;

                var client = _httpClientFactory.CreateClient();
                var converter = new Html2Markdown.Converter();
                string conteudoMarkdown = converter.Convert(noticia.Conteudo);
                string conteudoLimpo = TagsIndesejadasRegex().Replace(conteudoMarkdown, " ").Trim();
                
                if (conteudoLimpo.Length > 500) conteudoLimpo = string.Concat(conteudoLimpo.AsSpan(0, 500), "...");

                string urlImagemFinal = string.IsNullOrEmpty(noticia.ImagemUrl) 
                    ? "https://portal.dmplace.com.br/img/default.jpg" 
                    : $"https://portal.dmplace.com.br{noticia.ImagemUrl}";

                var payload = new
                {
                    username = "DM Place - Crônicas",
                    content = "📜 **Nova Crônica Publicada!**",
                    embeds = new[]
                    {
                        new
                        {
                            title = noticia.Titulo,
                            // ATENÇÃO: Link atualizado para o novo padrão /C/{slug}
                            description = conteudoLimpo + $"\n\n[Leia a crônica completa aqui](https://portal.dmplace.com.br/C/{campanha.NomeSimples}/Detalhes/{noticia.Id})",
                            color = settings.TemaCorPrimaria != null ? int.Parse(settings.TemaCorPrimaria.Replace("#", ""), System.Globalization.NumberStyles.HexNumber) : 16766720,
                            image = new { url = urlImagemFinal },
                            footer = new { text = $"🏷️ {noticia.Categoria} • {DateTime.Now:dd/MM HH:mm}" }
                        }
                    }
                };

                await client.PostAsJsonAsync(settings.DiscordWebhookUrl, payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discord Error] {ex.Message}");
            }
        }

        [HttpPost("TestarWebhook")]
        public async Task<IActionResult> TestarWebhook(int campanhaId)
        {
            try
            {
                // 1. Busca a campanha e as configurações (igual na sua função original)
                var campanha = await _context.Campanhas.FindAsync(campanhaId);
                var settings = await _context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == campanhaId);

                if (campanha == null || settings == null || string.IsNullOrEmpty(settings.DiscordWebhookUrl))
                {
                    Console.WriteLine("[Webhook Test] Webhook não configurado ou campanha inválida.");
                    return Json(new { success = false, message = "Webhook não configurado para esta campanha!" });
                }

                // 2. Cria uma notícia fake apenas para o teste
                var noticiaTeste = new Noticia
                {
                    Id = 0,
                    Titulo = "🛠️ Teste de Conexão Bem-Sucedido!",
                    Conteudo = "<p>O <b>Portal DM Place</b> conseguiu estabelecer o vínculo arcano com este canal. O corvo mensageiro está pronto para as próximas crônicas!</p>",
                    Categoria = "Sistema",
                    CampanhaId = campanhaId,
                    ImagemUrl = "/img/default.png" // Imagem padrão para o teste
                };

                // 3. Executa a lógica de envio (reutilizando sua lógica)
                await EnviarNoticiaDiscord(noticiaTeste);

                Console.WriteLine("[Webhook Test] Mensagem de teste enviada com sucesso.");

                return Json(new { success = true, message = "Corvo enviado! Verifique o canal do Discord." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Webhook Test Error] {ex.Message}");
                return Json(new { success = false, message = $"Falha no ritual: {ex.Message}" });
            }
        }
    }
}