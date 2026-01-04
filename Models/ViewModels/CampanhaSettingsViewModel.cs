namespace PortalDMPlace.Models.ViewModels
{
    public class CampanhaSettingsViewModel
    {
        // Dados da Campanha
        public int CampanhaId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NomeSimples { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Dados de Settings
        public int SettingsId { get; set; }
        public string? TemaCorPrimaria { get; set; }
        public string? TemaCorSecundaria { get; set; }
        public string? BannerUrl { get; set; }
        public string? CardThumbnailUrl { get; set; }
        public string? ChamadaCard { get; set; }
        public string? FonteFamilia { get; set; }
        public string? DiscordWebhookUrl { get; set; }
        public string? FoundryUrl { get; set; }
    }
}