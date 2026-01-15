using PortalDMPlace.Models;
using Microsoft.EntityFrameworkCore;

namespace PortalDMPlace.Functions
{
    public class HelpersFunctions(DataContext context)
    {
        public string GetCampaignNameById(int campaignId)
        {
            var campanha = context.Campanhas.FirstOrDefault(c => c.Id == campaignId);
            return campanha?.Name ?? "Usuário sem Reino";
        }

        public async Task<List<Campanha>> GetCampanhasAsync()
        {
            return await context.Campanhas
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
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
        
        // Formata data no estilo "dd 'de' MMM, yyyy" (ex: 05 de Mar, 2024)
        public static string FormatLoreDate(DateTime date)
        {
            return date.ToString("dd 'de' MMM, yyyy");
        }
    }
}