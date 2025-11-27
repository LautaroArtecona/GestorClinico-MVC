namespace WebApplication_GestorClinico.Models.Vistas
{
    public class AdminDashboard
    {
        // Totales Globales
        public int CantidadMedicos { get; set; }
        public int CantidadAdministrativos { get; set; }
        public int CantidadPacientes { get; set; }

        // Lista de estadísticas por centro
        public List<CentroEstadisticaDTO> EstadisticasPorCentro { get; set; } = new List<CentroEstadisticaDTO>();
    }

    // Clase auxiliar para los datos de cada centro
    public class CentroEstadisticaDTO
    {
        public string NombreBarrio { get; set; }
        public string Direccion { get; set; }

        // Métricas de Guardia de ESTE centro
        public int PacientesEnEspera { get; set; }
        public int AtendidosHoy { get; set; }
        public string DemoraPromedio { get; set; }
    }
}
