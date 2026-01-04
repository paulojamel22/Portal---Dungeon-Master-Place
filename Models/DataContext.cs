using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace PortalDMPlace.Models
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<Noticia> Noticias { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Campanha> Campanhas { get; set; }
        public DbSet<Settings> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Garante que cada campanha tenha apenas uma configuração ativa
            modelBuilder.Entity<Settings>().HasIndex(s => s.CampanhaId).IsUnique();
            
            // Configura o relacionamento 1:1 entre Campanha e Settings
            modelBuilder.Entity<Campanha>()
                .HasOne(c => c.Settings)
                .WithOne()
                .HasForeignKey<Settings>(s => s.CampanhaId);
        }
    }

    public partial class Noticia
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public string Autor { get; set; } = "Mestre";
        public DateTime DataPublicacao { get; set; } = DateTime.Now;
        public string Categoria { get; set; } = "Atualização";
        public string ImagemUrl { get; set; } = "./img/noticias/default.jpg";

        public int CampanhaId { get; set; }
        [ForeignKey("CampanhaId")]
        public virtual Campanha? Campanha { get; set; }

        public string GetResumo(int limite = 160)
        {
            if (string.IsNullOrWhiteSpace(Conteudo)) return string.Empty;
            string textoLimpo = GetResumoRegex().Replace(Conteudo, string.Empty);
            return textoLimpo.Length > limite ? string.Concat(textoLimpo.AsSpan(0, limite), "...") : textoLimpo;
        }

        [GeneratedRegex("<.*?>", RegexOptions.IgnoreCase)]
        private static partial Regex GetResumoRegex();
    }

    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Senha em texto (cuidado!)
        public string HashPassword { get; set; } = string.Empty;
        public int CampanhaId { get; set; }
    }

    public class Campanha
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NomeSimples { get; set; } = string.Empty; // Ex: "Aetheria", "Desafio"
        public string Description { get; set; } = string.Empty;
        
        // Atalho para facilitar o uso nas Views sem precisar de joins complexos
        public virtual Settings? Settings { get; set; }

        // Helper para as views que criamos
        [NotMapped]
        public string DiscordWebhookUrl => Settings?.DiscordWebhookUrl ?? string.Empty;
    }

    public class Settings
    {
        public int Id { get; set; }
        public int CampanhaId { get; set; }
        
        // Webhooks e Integrações
        public string DiscordWebhookUrl { get; set; } = string.Empty;
        public string FoundryUrl { get; set; } = "https://rpg.dmplace.com.br";

        // --- CUSTOMIZAÇÃO VISUAL ---
        public string TemaCorPrimaria { get; set; } = "#8e0000"; 
        public string TemaCorSecundaria { get; set; } = "#3a0000";
        public string FonteFamilia { get; set; } = "'Segoe UI', sans-serif";
        
        // Imagens dinâmicas
        public string? BannerUrl { get; set; } = "/img/banners/default.png";
        public string? CardThumbnailUrl { get; set; } = "/img/thumbnails/default.png";

        // --- TEXTOS DINÂMICOS DA INTERFACE ---
        public string? ChamadaCard { get; set; } = "Uma nova aventura começa.";
        public bool ExibirRelogioSessao { get; set; } = true;
    }
}