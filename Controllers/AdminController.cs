using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PortalDMPlace.Controllers
{
    public partial class AdminController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

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
        public IActionResult Criar([BindRequired, Bind("Id,Titulo,Conteudo,Autor,ImagemUrl,Campanha,Categoria")] Noticia noticia, IFormFile? ImagemFile)
        {
            ModelState.Remove("Conteudo");

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Verifique os campos obrigatórios antes de continuar.";
                return View(noticia);
            }

            try
            {
                // Define a data atual
                noticia.DataPublicacao = DateTime.Now;

                // Se tiver imagem enviada
                if (ImagemFile != null && ImagemFile.Length > 0)
                {
                    // Garante que a pasta existe
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens");
                    if (!Directory.Exists(uploadsPath))
                        Directory.CreateDirectory(uploadsPath);

                    // Gera nome único pra imagem
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImagemFile.FileName);
                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Salva o arquivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImagemFile.CopyTo(stream);
                    }

                    // Armazena o caminho relativo pra exibição no site
                    noticia.ImagemUrl = "/imagens/" + fileName;
                }

                // Garante autor padrão se não informado
                if (string.IsNullOrWhiteSpace(noticia.Autor))
                    noticia.Autor = "Mestre";

                // Salva no banco
                _context.Noticias.Add(noticia);
                _context.SaveChanges();

                // Após _context.SaveChanges();
                EnviarNoticiaDiscord(noticia);

                TempData["Sucesso"] = "Notícia publicada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erro ao salvar a notícia: " + ex.Message;
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
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new List<string> { "Atualização", "Evento", "Diário de Sessão", "Rumor" };
                return View(noticia);
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

                _context.SaveChanges();
                TempData["Sucesso"] = "Notícia atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erro ao atualizar: " + ex.Message;
                ViewBag.Categorias = new List<string> { "Avisos", "Atualizações", "Eventos", "Lore" };
                return View(noticia);
            }
        }

        // --- Login ---
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.Accounts.FirstOrDefault(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
            {
                ViewBag.Erro = "Usuário ou senha inválidos.";
                return View();
            }

            HttpContext.Session.SetString("Logado", "true");
            HttpContext.Session.SetInt32("Campanha", user.Campanha);
            HttpContext.Session.SetString("Usuario", user.Username);

            return RedirectToAction("Index", "Admin");
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

        // --- Logout ---
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private static async void EnviarNoticiaDiscord(Noticia noticia)
        {
            try
            {
                string webhookUrl = "SEU WEBHOOK DISCORD AQUI";

                using var httpClient = new HttpClient();

                // 1. Crie uma instância do conversor
                var converter = new Html2Markdown.Converter();

                // 2. Converta o conteúdo HTML para Markdown
                string conteudoMarkdown = converter.Convert(noticia.Conteudo);

                // 3. Aplique o limite de caracteres do Discord (máx. 4096 para embeds)
                // Usamos um limite seguro para o snippet e depois cortamos.
                const int limiteDiscord = 4096;
                string descricaoFormatada = conteudoMarkdown.Length > 250
                    ? conteudoMarkdown[..Math.Min(conteudoMarkdown.Length, limiteDiscord)] // Corta para o limite do Discord
                    : conteudoMarkdown;

                // Limpar tags problemáticas
                string conteudoLimpo = TagsIndesejadasRegex().Replace(descricaoFormatada, "");

                // Também remove espaços extras e quebras de linha
                conteudoLimpo = TagsIndesejadasRegex().Replace(conteudoLimpo, " ").Trim();

                if (conteudoLimpo.Length > 250) // Se for maior que 500, adiciona "..."
                {
                    conteudoLimpo = string.Concat(conteudoLimpo.AsSpan(0, 500), "...");
                }

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
                                color = noticia.Campanha == 1 ? 0xFFD700 : 0x8B0000, // Aetheria = dourado / Desafio de Sangue = vermelho escuro
                                url = $"https://portal.dmplace.com.br/Aetheria/Detalhes/{noticia.Id}",
                                image = string.IsNullOrEmpty(noticia.ImagemUrl) ? null : new { url = $"https://portal.dmplace.com.br{noticia.ImagemUrl}" },
                                footer = new { text = $"{noticia.Categoria} • Publicado por {noticia.Autor} em {noticia.DataPublicacao:dd/MM/yyyy HH:mm}" }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await httpClient.PostAsync(webhookUrl, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao enviar notícia ao Discord: " + ex.Message);
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
    }
}
