namespace WebApplication_GestorClinico.Models
{
    public class HistoriaClinica : IEliminable
    {
        public int Id { get; set; }


        // FK hacia Paciente

        public int PacienteId { get; set; }

        // Relación 1-a-1 con Paciente
        public virtual Paciente Paciente { get; set; }

        //El historial de órdenes médicas
        public virtual ICollection<OrdenMedica> OrdenesMedicas { get; set; }

        //El historial de recetas
        public virtual ICollection<Receta> Recetas { get; set; }

        //El historial de evoluciones
        public virtual ICollection<EvolucionMedica> EvolucionesMedicas { get; set; }

        // Interfaz de eliminacion logica
        public bool Activo { get; set; } = true;
    }
}
