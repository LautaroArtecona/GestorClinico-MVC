namespace WebApplication_GestorClinico.Models
{
    public class HistoriaClinica
    {
        public int Id { get; set; }

        // (Aquí puedes agregar otros datos, como 'GrupoSanguineo', 'Alergias', etc.)

        // --- Relación 1-a-1 con Paciente ---
        // (La llave foránea está en Paciente, así que aquí solo navegamos)
        public virtual Paciente Paciente { get; set; }

        // --- ¡AQUÍ ESTÁ TU LÓGICA! ---
        // --- Propiedades de Navegación Inversas (1-N) ---

        // 1. El historial de órdenes médicas
        public virtual ICollection<OrdenMedica> OrdenesMedicas { get; set; }

        // 2. El historial de recetas
        public virtual ICollection<Receta> Recetas { get; set; }

        // 3. El historial de evoluciones
        public virtual ICollection<EvolucionMedica> EvolucionesMedicas { get; set; }
    }
}
