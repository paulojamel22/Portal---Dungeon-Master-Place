using System.ComponentModel.DataAnnotations;
using PortalDMPlace.Enumerators;

namespace PortalDMPlace.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome de usuário é obrigatório")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "O usuário deve ter entre 4 e 20 caracteres")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "As senhas não coincidem")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escolha seu caminho no reino")]
        public AccountType AccountType { get; set; }

        public int CampanhaId { get; set; }
    }
}