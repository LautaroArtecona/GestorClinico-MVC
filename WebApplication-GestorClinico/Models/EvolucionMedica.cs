namespace WebApplication_GestorClinico.Models
{
    public class EvolucionMedica
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Diagnostico { get; set; }
        public string Tratamiento { get; set; }
        public string Observacion { get; set; }

        // --- Clave Foránea (FK) ---
        // (Le dice a qué historial pertenece esta evolución)
        public int HistoriaClinicaId { get; set; }
        public virtual HistoriaClinica HistoriaClinica { get; set; }
    }
}
