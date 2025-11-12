using System.Security.Permissions;

namespace WebApplication_GestorClinico.Models
{
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        // Relacion N-N
        public virtual ICollection<Usuario> Usuarios { get; set; }
    }
}
