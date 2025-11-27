using System.ComponentModel.DataAnnotations;

namespace WebApplication_GestorClinico.Models.Vistas
{
    public class AtencionGuardia
    {
        // Datos para identificar el proceso
        public int IdCola { get; set; } // El ID del registro en PacientesEnEspera
        public int PacienteId { get; set; }

        // Datos Informativos (Solo lectura para mostrar al médico)
        public string NombrePaciente { get; set; }
        public string Dni { get; set; }
        public string ObraSocial { get; set; }

        // Campos de la Evolución (Lo que llena el médico)
        [Required(ErrorMessage = "El diagnóstico es obligatorio")]
        public string Diagnostico { get; set; }

        [Required(ErrorMessage = "El tratamiento es obligatorio")]
        public string Tratamiento { get; set; }

        [Required]
        public string Observacion { get; set; }



        // LISTAS PARA ESTUDIOS Y RECETAS
        // el medico puede llenarlo dinamicamente
        public List<OrdenMedicaDTO> Ordenes { get; set; } = new List<OrdenMedicaDTO>();
        public List<RecetaDTO> Recetas { get; set; } = new List<RecetaDTO>();
    }

    // Clases auxiliares para transportar los datos de la lista
    public class OrdenMedicaDTO
    {
        public string NombreEstudio { get; set; }
        public string Diagnostico { get; set; } 
    }

    public class RecetaDTO
    {
        public string Medicamento { get; set; }
        public string Dosis { get; set; }
        public int Cantidad { get; set; }
    }
}
