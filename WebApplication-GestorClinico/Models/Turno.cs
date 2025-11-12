namespace WebApplication_GestorClinico.Models
{
    public class Turno
    {
        public int Id { get; set; }

        // --- Atributos del Turno ---

        // Fecha y Hora de inicio del turno
        public DateTime FechaHoraInicio { get; set; }

        // Duración en minutos (ej: 15, 30)
        public int DuracionEnMinutos { get; set; }

        // Estado del turno (EF lo inicializa en 'false' por defecto)
        public bool Atendido { get; set; }

        // --- Relaciones (Claves Foráneas) ---

        // 1. Dónde (El Centro Médico)
        public int CentroMedicoId { get; set; }
        public virtual CentroMedico CentroMedico { get; set; }

        // 2. Quién (El Médico que atiende)
        public int MedicoId { get; set; }
        public virtual Medico Medico { get; set; }

        // 3. Qué (La Especialidad)
        // (Como dijiste, esto es redundante pero BUENO para performance)
        public int EspecialidadId { get; set; }
        public virtual Especialidad Especialidad { get; set; }

        // 4. Para quién (El Paciente)
        // ¡La '?' (nullable) es la clave!
        // Si PacienteId es NULL, el turno está libre.
        // Si tiene un valor, está tomado.
        public int? PacienteId { get; set; }
        public virtual Paciente Paciente { get; set; }
    }
}
