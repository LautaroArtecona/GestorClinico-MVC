namespace WebApplication_GestorClinico.Models
{
    public class Guardia
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        // Relación 1-N con CentroMedico
        public int CentroMedicoId { get; set; }
        public virtual CentroMedico CentroMedico { get; set; }

        // Relación 1-N con ColaDePacientes
        public virtual ICollection<PacienteEnEspera> PacientesEnEspera { get; set; } 

    }
}
