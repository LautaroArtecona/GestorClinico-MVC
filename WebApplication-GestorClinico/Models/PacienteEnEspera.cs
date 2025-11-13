namespace WebApplication_GestorClinico.Models
{
    public class PacienteEnEspera
    {
        public int Id { get; set; }
        public DateTime HoraDeIngreso { get; set; }

        public DateTime? HoraAtencion { get; set; }


        // Relación N-1 con Guardia
        // Este PacienteEnEspera pertenece a UNA Guardia
        public int GuardiaId { get; set; }
        public virtual Guardia Guardia { get; set; }

        // Relación N-1 con Paciente
        public int PacienteId { get; set; }
        public virtual Paciente Paciente { get; set; }

        // estado de atencion
        public int EstadoId { get; set; }
        public virtual Estado Estado { get; set; }

    }
}