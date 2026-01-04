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
    [Authorize] // Garante que toda a gestão de notícias exija login
    [Route("Admin/Noticias")] // Define a rota base para o controller
    public partial class NoticiaController(DataContext context, IHttpClientFactory httpClientFactory) : Controller
    {
        private readonly DataContext _context = context;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private HelpersFunctions Functions => new(_context);

        [GeneratedRegex(@"<\/?(div|article|span|cite)[^>]*>", RegexOptions.IgnoreCase)]
        private static partial Regex TagsIndesejadasRegex();

        // --- LISTAGEM ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var noticias = await _context.Noticias
                .Include(n => n.Campanha) // Importante para mostrar o nome da campanha na tabela
                .OrderByDescending(n => n.DataPublicacao)
                .ToListAsync();

            return View("~/Views/Admin/Noticias/Index.cshtml", noticias);
        }

        // --- CRIAÇÃO ---
        [HttpGet("Criar")]
        public IActionResult Criar()
        {
            var name = _context.Accounts.FirstOrDefault()?.Name;
            ViewBag.Campanhas = _context.Campanhas.ToList(); // Necessário para o Select de campanhas
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            ViewBag.Name = name;
            
            return View("~/Views/Admin/Noticias/Criar.cshtml");
        }

        [HttpPost("Criar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Noticia noticia, IFormFile? ImagemFile)
        {
            ModelState.Remove("Conteudo");
            ModelState.Remove("Campanha"); // Evita erro por não enviar o objeto Campanha inteiro

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImagemFile != null && ImagemFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(ImagemFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", fileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImagemFile.CopyToAsync(stream);
                        }
                        noticia.ImagemUrl = "/img/noticias/" + fileName;
                    }

                    noticia.DataPublicacao = DateTime.Now;
                    _context.Noticias.Add(noticia);
                    await _context.SaveChangesAsync();

                    await EnviarNoticiaDiscord(noticia);

                    TempData["Sucesso"] = "Crônica publicada nos anais do tempo!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Erro ao forjar notícia: " + ex.Message;
                }
            }

            ViewBag.Campanhas = _context.Campanhas.ToList();
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            return View("~/Views/Admin/Noticias/Criar.cshtml", noticia);
        }

        // --- EDIÇÃO ---
        [HttpGet("Editar/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia == null) return NotFound();

            ViewBag.Campanhas = _context.Campanhas.ToList();
            ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
            return View("~/Views/Admin/Noticias/Editar.cshtml", noticia);
        }

        [HttpPost("Editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Noticia noticia, IFormFile? ImagemFile)
        {
            ModelState.Remove("ImagemUrl");
            ModelState.Remove("Campanha");

            if (!ModelState.IsValid)
            {
                ViewBag.Campanhas = _context.Campanhas.ToList();
                ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
                return View("~/Views/Admin/Noticias/Editar.cshtml", noticia);
            }

            var noticiaExistente = await _context.Noticias.FindAsync(noticia.Id);
            if (noticiaExistente == null) return NotFound();

            if(id != noticia.Id) return BadRequest();

            noticiaExistente.Titulo = noticia.Titulo;
            noticiaExistente.Conteudo = noticia.Conteudo;
            noticiaExistente.CampanhaId = noticia.CampanhaId;
            noticiaExistente.Categoria = noticia.Categoria;
            noticiaExistente.Autor = noticia.Autor;

            if (ImagemFile != null && ImagemFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImagemFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/noticias", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImagemFile.CopyToAsync(stream);
                }
                noticiaExistente.ImagemUrl = "/img/noticias/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Crônica reescrita com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Noticias/Detalhes/5
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
        [HttpPost("Excluir/{id}")] // Melhor usar Post para exclusão por segurança
        public async Task<IActionResult> Excluir(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia != null)
            {
                _context.Noticias.Remove(noticia);
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Crônica apagada da história.";
            }
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
    }
}