namespace WebApplication_GestorClinico.Models.Vistas
{
    public class PacienteDashboard
    {
        public string NombreCompleto { get; set; }
        public int TurnosPendientes { get; set; }
        public Turno? ProximoTurno { get; set; } // Puede ser null
    }
}
