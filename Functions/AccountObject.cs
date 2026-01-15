using PortalDMPlace.Enumerators;
using PortalDMPlace.Models;

namespace PortalDMPlace.Functions
{
    public class AccountObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = "/img/profiles/default.png";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime BirthDate { get; set; } = DateTime.Now.AddYears(-18);
        public AccountType AccountType { get; set; } = AccountType.Player;
        public int CampanhaId { get; set; }

        public bool IsAuthenticated => Id > 0;

        public void LoadUserData(Account account)
        {
            this.Id = account.Id;
            this.Name = account.Name;
            this.Username = account.Username;
            this.ProfileImageUrl = account.ProfileImageUrl;
            this.CreatedAt = account.CreatedAt;
            this.BirthDate = account.BirthDate;
            this.AccountType = account.AccountType;
            this.CampanhaId = account.CampanhaId;
        }

        public void UnloadUserData()
        {
            this.Id = 0;
            this.Name = string.Empty;
            this.Username = string.Empty;
            this.ProfileImageUrl = "/img/profiles/default.png";
            this.CreatedAt = DateTime.Now;
            this.BirthDate = DateTime.Now.AddYears(-18);
            this.AccountType = AccountType.Player;
            this.CampanhaId = 0;
        }

        // Verifica se o usuário tem NO MÍNIMO o nível solicitado
        public bool HasAccess(AccountType requiredLevel)
        {
            return (int)this.AccountType >= (int)requiredLevel;
        }

        // Atalhos para facilitar a leitura no código
        public bool IsAtLeastMaster => (int)AccountType >= (int)AccountType.Master;
        public bool IsAtLeastAdmin => (int)AccountType >= (int)AccountType.Administrator;
        public bool IsDev => AccountType == AccountType.Developer;
        // No AccountObject.cs ou na View
        public string GetPowerColor() => AccountType switch
        {
            AccountType.Developer => "#6f42c1", // Roxo (Arcano)
            AccountType.Administrator => "#ffc107", // Dourado (Realeza)
            AccountType.Master => "#dc3545", // Vermelho (Sangue)
            _ => "#6c757d" // Cinza (Aço)
        };
    }
}