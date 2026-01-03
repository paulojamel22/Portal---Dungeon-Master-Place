using PortalDMPlace.Models;

namespace PortalDMPlace.Functions
{
    public class Functions(DataContext context)
    {
        public string GetCampaignNameById(int campaignId)
        {
            // Lógica para obter o nome da campanha pelo ID
            var campanha = context.Campanhas.FirstOrDefault(c => c.Id == campaignId);

            return campanha?.NomeSimples ?? $"Home";
        }
    }
}