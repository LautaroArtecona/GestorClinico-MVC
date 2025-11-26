namespace WebApplication_GestorClinico.Models.Vistas
{
    public class MedicoDashboard
    {
        public string NombreMedico { get; set; }
        public DateTime? ProximaFecha { get; set; } // Puede ser null si no tiene agenda
        public int TurnosLibres { get; set; }
        public int TurnosAsignados { get; set; }
        public string HorarioInicio { get; set; } 
        public string HorarioFin { get; set; }    
        public bool TieneAgenda { get; set; }     
    }
}
