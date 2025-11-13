using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
//using System.Xml.Linq;

namespace WebApplication_GestorClinico.Models
{
    public class CentroMedico : IEliminable
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

        public bool Activo { get; set; } = true;
    }
}
