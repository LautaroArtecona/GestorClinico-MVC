namespace WebApplication_GestorClinico.Models
{
    public class Clinica
    {
        public int Id { get; set; } 
        public string Nombre { get; set; }

        //Aca se referencia a que la clinica tiene un listado de pacientes, medicos y administrativos
        public virtual ICollection<Paciente> Pacientes { get; set; }

        public virtual ICollection<Medico> Medicos { get; set; }

        public virtual ICollection<Administrativo> Administrativos { get; set; }

        // Aca se referencia a que la clinica cuenta con Centros medicos
        public virtual ICollection<CentroMedico> CentrosMedicos { get; set; }
    }
}
