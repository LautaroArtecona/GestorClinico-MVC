namespace WebApplication_GestorClinico.Models.Vistas
{
    public class CancelarAgenda
    {
        public DateTime Fecha { get; set; }
        public List<Turno> Turnos { get; set; } = new List<Turno>();

        // Helpers visuales
        public int CantidadTotal => Turnos.Count;
        public int CantidadConPaciente => Turnos.Count(t => t.PacienteId != null);
        public string HoraInicio => Turnos.Min(t => t.FechaHoraInicio).ToString("HH:mm");
        public string HoraFin => Turnos.Max(t => t.FechaHoraInicio).AddMinutes(Turnos.First().DuracionEnMinutos).ToString("HH:mm");
    }
}
