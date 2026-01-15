using Microsoft.EntityFrameworkCore;
using PortalDMPlace.Enumerators;
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
        public DbSet<GlobalSettings> GlobalSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- Relacionamentos de Campanha ---
            
            // Garante que cada campanha tenha apenas uma configuração ativa (1:1)
            modelBuilder.Entity<Settings>().HasIndex(s => s.CampanhaId).IsUnique();
            modelBuilder.Entity<Campanha>()
                .HasOne(c => c.Settings)
                .WithOne()
                .HasForeignKey<Settings>(s => s.CampanhaId);

            // Relacionamento Campanha -> Criador (Dono)
            modelBuilder.Entity<Campanha>()
                .HasOne(c => c.Criador)
                .WithMany()
                .HasForeignKey(c => c.CriadorId)
                .OnDelete(DeleteBehavior.Restrict); // Não apaga o mestre se apagar a campanha

            // --- Relacionamentos de Noticia ---

            // Relacionamento Noticia -> Autor
            modelBuilder.Entity<Noticia>()
                .HasOne(n => n.AutorAccount)
                .WithMany()
                .HasForeignKey(n => n.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamento Noticia -> Campanha
            modelBuilder.Entity<Noticia>()
                .HasOne(n => n.Campanha)
                .WithMany()
                .HasForeignKey(n => n.CampanhaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public partial class Noticia
    {
        public int Id { get; set; }
        
        public int AccountId { get; set; } // ID do autor (Chave Estrangeira)
        [ForeignKey("AccountId")]
        public virtual Account? AutorAccount { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public string Autor { get; set; } = "Mestre"; // Nome amigável para exibição rápida
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
        public string Password { get; set; } = string.Empty; 
        public string HashPassword { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = "/img/profiles/default.png";
        public DateTime CreatedAt { get; set; }
        public DateTime BirthDate { get; set; }
        public AccountType AccountType { get; set; } = AccountType.Player;
        public int CampanhaId { get; set; } // Campanha principal vinculada
    }

    public class Campanha
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NomeSimples { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;
        
        public virtual Settings? Settings { get; set; }

        public int CriadorId { get; set; } // Chave Estrangeira do Mestre
        [ForeignKey("CriadorId")]
        public virtual Account? Criador { get; set; }

        [NotMapped]
        public string DiscordWebhookUrl => Settings?.DiscordWebhookUrl ?? string.Empty;
    }

    public class Settings
    {
        public int Id { get; set; }
        public int CampanhaId { get; set; }
        public string DiscordWebhookUrl { get; set; } = string.Empty;
        public string VttUrl { get; set; } = "SeuServidorVTTAqui";
        public string TemaCorPrimaria { get; set; } = "#8e0000"; 
        public string TemaCorSecundaria { get; set; } = "#3a0000";
        public string FonteFamilia { get; set; } = "'Segoe UI', sans-serif";
        public string? BannerUrl { get; set; } = "/img/banners/default.png";
        public string? CardThumbnailUrl { get; set; } = "/img/thumbnails/default.png";
        public string? ChamadaCard { get; set; } = "Uma nova aventura começa.";
        public bool ExibirRelogioSessao { get; set; } = true;
    }

    public class GlobalSettings
    {
        [Key]
        public int Id { get; set; } = 1;
        public bool ManutencaoAtiva { get; set; } = false;
        public string MensagemManutencao { get; set; } = "O portal está em manutenção.";

        //=========================================================
        // Configurações globais do portal. - Migration não gerada
        //=========================================================
        // public string PortalNome { get; set; } = "Seu Portal de RPG";
        // public string PortalDescricao { get; set; } = "O melhor lugar para gerenciar suas campanhas de RPG.";
        // public string PortalLogoUrl { get; set; } = "/img/logo/portal-logo.png";
        // public string PortalFaviconUrl { get; set; } = "/img/logo/portal-favicon.ico";
        // public string PortalUrlBase { get; set; } = "http://localhost:5000";
        // public string PortalCorFundo { get; set; } = "#121212";
        // public string PortalCorPrimaria { get; set; } = "#6b2d90";
        // public string PortalCorSecundaria { get; set; } = "#4a1a6e";
        // public string PortalFonteFamilia { get; set; } = "'Segoe UI', sans-serif";
        // public int MaxNoticiasExibir { get; set; } = 5;
        // public int MaxAccountsPorCampanha { get; set; } = 20;
        // public int MaxCampanhasPorMestre { get; set; } = 10;
        // public bool PermitirCadastroPublico { get; set; } = true;
    }
}