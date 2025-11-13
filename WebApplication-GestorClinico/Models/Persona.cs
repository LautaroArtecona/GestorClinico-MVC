namespace WebApplication_GestorClinico.Models
{
    public abstract class Persona : IEliminable
    {
        public string Dni { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }

        // cada persona tiene un usuario
        public int? UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }

        // las personas estan en la clase clinica
        public int ClinicaId { get; set; }
        public virtual Clinica Clinica { get; set; }
        public bool Activo { get; set; } = true;
    }
}
