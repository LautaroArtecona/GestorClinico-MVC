using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
//using System.Xml.Linq;

namespace WebApplication_GestorClinico.Models
{
    public class CentroMedico
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Barrio { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }

        // Se refencia que el Centro Medico pertenece a una Clinica
        public int ClinicaId { get; set; }
        public virtual Clinica Clinica { get; set; }

        // Coleccion de guardias
        public virtual ICollection<Guardia> Guardias { get; set; }

        // Un Centro Medico tiene MUCHOS Turnos
        public virtual ICollection<Turno> Turnos { get; set; }

        /*
        public string Nombre { get; set; }

        public int Edad { get; set; }

        [Display(Name = "Fecha inscripción")]
        public DateTime FechaInscripto { get; set; }

        [EnumDataType(typeof(TipoUsuario))]
        public TipoUsuario TipoDeUsuario { get; set; }
        */
    }
}
