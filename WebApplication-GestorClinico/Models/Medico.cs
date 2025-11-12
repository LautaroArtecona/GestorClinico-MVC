namespace WebApplication_GestorClinico.Models
{
    public class Medico : Persona
    {
        public int Id { get; set; }
        public string Matricula { get; set; }

        // Relacion 1 a 1 con Especialidad
        public int EspecialidadId { get; set; }
        public virtual Especialidad Especialidad { get; set; }

        // Un Medico tiene MUCHOS Turnos (su agenda)
        public virtual ICollection<Turno> Turnos { get; set; }

    }
}
