using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PortalDMPlace.Models; // Adicione este namespace

namespace PortalDMPlace.Controllers
{
    [Microsoft.AspNetCore.Cors.EnableCors("AllowFoundry")]
    [ApiController]
    [Route("api/[controller]")]
    public class IntegrationController(DataContext context, IMemoryCache cache) : ControllerBase
    {
        private readonly DataContext _context = context;
        private readonly IMemoryCache _cache = cache;

        // Classe para garantir o mapeamento correto do JSON
        public class FoundryWorldData
        {
            public string NomeMundo { get; set; } = null!;
            public string Descricao { get; set; } = null!;
        }

        [HttpPost("ConectarFoundry")]
        public IActionResult ConectarFoundry([FromBody] FoundryWorldData dados)
        {
            try
            {
                if (dados == null || string.IsNullOrEmpty(dados.NomeMundo))
                {
                    return BadRequest(new { mensagem = "Dados do mundo não fornecidos." });
                }

                // Armazena no Cache
                _cache.Set("Foundry_NomeMundo", dados.NomeMundo, TimeSpan.FromHours(1));
                _cache.Set("Foundry_UltimoSinal", DateTime.Now, TimeSpan.FromHours(1));

                return Ok(new { 
                    status = "Visualizado", 
                    mensagem = $"Mestre, recebi os dados de {dados.NomeMundo} com sucesso!" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar dados do Foundry: {ex.Message}");
                return BadRequest($"Falha na leitura do oráculo: {ex.Message}");
            }
        }

        [HttpGet("UltimaSessao")]
        public async Task<IActionResult> GetUltimaSessao()
        {
            var noticia = await _context.Noticias
                .Where(n => n.Categoria == "Diário de Sessão")
                .OrderByDescending(n => n.DataPublicacao)
                .Select(n => new {
                    titulo = n.Titulo,
                    conteudo = n.Conteudo, // Certifique-se de que o texto não seja longo demais para o modal
                    data = n.DataPublicacao.ToString("dd/MM/yyyy")
                })
                .FirstOrDefaultAsync();

            if (noticia == null) return NotFound();

            return Ok(noticia);
        }
    }
}