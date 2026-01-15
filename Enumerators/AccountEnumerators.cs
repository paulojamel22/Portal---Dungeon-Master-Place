namespace PortalDMPlace.Enumerators
{
    public enum AccountType
    {
        Player = 1,
        Master = 2,
        Administrator = 3,
        Developer = 99
    }

    public static class AccountTypeExtensions
    {
        public static string ToFriendlyString(this AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Player => "Player",
                AccountType.Master => "Master",
                AccountType.Administrator => "Administrator",
                AccountType.Developer => "Developer",
                _ => "Unknown"
            };
        }
    }
}