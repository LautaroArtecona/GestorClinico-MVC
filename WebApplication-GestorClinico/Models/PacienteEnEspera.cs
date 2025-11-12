namespace WebApplication_GestorClinico.Models
{
    public class PacienteEnEspera
    {
        public int Id { get; set; }

        // --- Relación N-1 con Guardia ---
        // (Este PacienteEnEspera pertenece a UNA Guardia)
        public int GuardiaId { get; set; }
        public virtual Guardia Guardia { get; set; }

        // --- Relación N-1 con Paciente ---
        // (Este PacienteEnEspera pertenece a UN Paciente)
        public int PacienteId { get; set; }
        public virtual Paciente Paciente { get; set; }


        // --- ¡LA LÓGICA DE LA COLA! ---

        // 1. El campo para "Enqueue" (FIFO)
        // Se guarda la hora exacta en que se agregó a la cola.
        public DateTime HoraDeIngreso { get; set; }

        // 2. El campo para "Dequeue"
        // Es 'nullable' (con '?'). Mientras sea 'null',
        // el paciente sigue en la cola.
        public DateTime? HoraAtencion { get; set; }

        // Por defecto, será 'false'
        public bool AbandonoVoluntario { get; set; }
    }
}