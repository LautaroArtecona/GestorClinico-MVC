namespace WebApplication_GestorClinico.Models
{
    public class Usuario : IEliminable
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Contrasenia { get; set; }

        // FK a Rol
        public int RolId { get; set; }
        public virtual Rol Rol { get; set; }

        // Un Usuario puede "ser" un Paciente / Medico / Administrativo
        public virtual Medico Medico { get; set; }
        public virtual Administrativo Administrativo { get; set; }
        public virtual Paciente Paciente { get; set; }
        public bool Activo { get; set; } = true;
    }
}