namespace WebApplication_GestorClinico.Models
{
    public class Especialidad : IEliminable
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        public virtual ICollection<Medico>? Medicos { get; set; }

        public bool Activo { get; set; } = true;
    }
}
