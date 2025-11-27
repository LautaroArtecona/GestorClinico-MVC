namespace WebApplication_GestorClinico.Models
{
    public class Receta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int Cantidad { get; set; }
        public string Medicamento { get; set; }
        public string Dosis { get; set; }

        // Clave Foránea
        // Le dice a qué historial pertenece esta receta
        public int HistoriaClinicaId { get; set; }
        public virtual HistoriaClinica HistoriaClinica { get; set; }
    }
}
