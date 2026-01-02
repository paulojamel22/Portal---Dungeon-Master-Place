using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace PortalDMPlace.Models
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<Noticia> Noticias { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Campanha> Campanhas { get; set; }
    }

    public partial class Noticia
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public string Autor { get; set; } = "Mestre";
        public DateTime DataPublicacao { get; set; } = DateTime.Now;
        public string Categoria { get; set; } = "Atualização";
        public string ImagemUrl { get; set; } = string.Empty;
        public int Campanha { get; set; }


        // 🔽 Novo método helper
        public string GetResumo(int limite = 160)
        {
            if (string.IsNullOrWhiteSpace(Conteudo))
                return string.Empty;

            // Remove as tags HTML
            string textoLimpo = GetResumoRegex().Replace(Conteudo, string.Empty);

            // Limita o texto
            return textoLimpo.Length > limite
                ? string.Concat(textoLimpo.AsSpan(0, limite), "...")
                : textoLimpo;
        }

        [GeneratedRegex("<.*?>", RegexOptions.IgnoreCase)]
        private static partial Regex GetResumoRegex();
    }

    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string HashPassword { get; set; } = string.Empty;
        public int Campanha { get; set; }
    }

    public class Campanha
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NomeSimples { get; set; } = string.Empty;
        public string Description {  get; set; } = string.Empty;
    }
}
