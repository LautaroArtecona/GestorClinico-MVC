using System.ComponentModel.DataAnnotations;

namespace WebApplication_GestorClinico.Models.Vistas
{
    public class GeneracionAgenda
    {
        [Required]
        public int MedicoId { get; set; }

        [Required]
        public int CentroMedicoId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaDesde { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaHasta { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; } // Ej: 08:00

        [Required]
        public TimeSpan HoraFin { get; set; }    // Ej: 14:00

        [Required]
        public int DuracionMinutos { get; set; } // 15, 30, 60

        // Checkbox para definir los dias de atencion
        public List<int> DiasSeleccionados { get; set; } = new List<int>();
    }
}
