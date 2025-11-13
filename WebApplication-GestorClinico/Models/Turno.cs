namespace WebApplication_GestorClinico.Models
{
    public class Turno : IEliminable
    {
        public int Id { get; set; }

        public DateTime FechaHoraInicio { get; set; }

        public int DuracionEnMinutos { get; set; }


        // --- Relaciones (Claves Foráneas) ---
        // Estado de atencion
        public int EstadoId { get; set; }
        public virtual Estado Estado { get; set; }

        public bool Activo { get; set; } = true;

        public int CentroMedicoId { get; set; }
        public virtual CentroMedico CentroMedico { get; set; }

        public int MedicoId { get; set; }
        public virtual Medico Medico { get; set; }

        public int EspecialidadId { get; set; }
        public virtual Especialidad Especialidad { get; set; }

        public int? PacienteId { get; set; }
        public virtual Paciente Paciente { get; set; }
    }
}
