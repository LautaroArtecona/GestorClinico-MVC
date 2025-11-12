namespace WebApplication_GestorClinico.Models
{
    public class OrdenMedica
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreEstudio { get; set; }
        public string Diagnostico { get; set; }

        // --- Clave Foránea (FK) ---
        // (Le dice a qué historial pertenece esta orden)
        public int HistoriaClinicaId { get; set; }
        public virtual HistoriaClinica HistoriaClinica { get; set; }
    }
}
