using System.ComponentModel.DataAnnotations;

namespace WebApplication_GestorClinico.Models.Vistas
{
    public class Login
    {
        [Required(ErrorMessage = "Debes ingresar tu usuario (DNI, Matrícula o Legajo).")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Debes ingresar tu contraseña.")]
        [DataType(DataType.Password)] // Esto ayuda al navegador a saber que es un password
        public string Password { get; set; }
    }
}
