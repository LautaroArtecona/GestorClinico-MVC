namespace WebApplication_GestorClinico.Models
{
    public class Paciente : Persona
    {
        public int Id { get; set; }
        public string ObraSocial { get; set; }
    

        public virtual HistoriaClinica HistoriaClinica { get; set; }

        // su historial de guardias
        public virtual ICollection<PacienteEnEspera>? RegistrosEnCola { get; set; }

        // Un Paciente tiene MUCHOS Turnos (tomados)
        public virtual ICollection<Turno>? Turnos { get; set; }
    }
}