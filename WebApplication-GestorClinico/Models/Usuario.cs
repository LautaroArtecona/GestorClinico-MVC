namespace WebApplication_GestorClinico.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Contrasenia { get; set; }

        // Esta es la "Foreign Key" que apunta a la tabla Roles
        public int RolId { get; set; }
        public virtual Rol Rol { get; set; }

        // Esto le dice a EF que un Usuario puede "ser" un Paciente (o un Medico, etc.)
        public virtual Medico Medico { get; set; }
        public virtual Administrativo Administrativo { get; set; }
        public virtual Paciente Paciente { get; set; }
    }
}