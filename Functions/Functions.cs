using PortalDMPlace.Models;
using Microsoft.EntityFrameworkCore;

namespace PortalDMPlace.Functions
{
    public class HelpersFunctions(DataContext context)
    {
        // Obtém o nome da rota (NomeSimples) para links dinâmicos
        public string GetCampaignNameById(int campaignId)
        {
            var campanha = context.Campanhas.FirstOrDefault(c => c.Id == campaignId);
            return campanha?.NomeSimples ?? "Home";
        }

        // NOVO: Busca as configurações visuais de uma campanha
        public async Task<Settings> GetSettingsAsync(int campaignId)
        {
            return await context.Settings.FirstOrDefaultAsync(s => s.CampanhaId == campaignId) 
                   ?? new Settings(); // Retorna padrão se não houver
        }

        // Define Variáveis CSS Dinâmicas para serem usadas no Style do HTML
        public static string GetInlineStyle(Settings set) 
        {
            return $"--primary-color: {set.TemaCorPrimaria}; --secondary-color: {set.TemaCorSecundaria}; --font-main: {set.FonteFamilia};";
        }

        // Helper para o Card da Home - Define qual imagem mostrar
        public static string GetThumbnail(Settings set)
        {
            return string.IsNullOrEmpty(set.CardThumbnailUrl) ? "/img/default_card.jpg" : set.CardThumbnailUrl;
        }

        public static string GenerateSlug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "home";
            // Melhorando o replace para ser mais robusto
            return name.ToLower().Trim().Replace(" ", "-");
        }

        public static string GetGlowClass(int campaignId)
        {
            return campaignId switch
            {
                1 => "glow-gold",
                2 => "glow-blood",
                _ => "glow-default"
            };
        }

        public static string FormatLoreDate(DateTime date)
        {
            return date.ToString("dd 'de' MMM, yyyy");
        }
    }
}