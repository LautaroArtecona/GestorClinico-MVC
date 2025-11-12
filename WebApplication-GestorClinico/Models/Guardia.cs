namespace WebApplication_GestorClinico.Models
{
    public class Guardia
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        // --- Relación 1-N con CentroMedico ---
        // (Esta Guardia pertenece a UN CentroMedico)
        public int CentroMedicoId { get; set; }
        public virtual CentroMedico CentroMedico { get; set; }

        // --- Relación 1-N con ColaDePacientes ---
        // (Esta Guardia tiene MUCHOS pacientes en espera)
        public virtual ICollection<PacienteEnEspera> PacientesEnEspera { get; set; } 

    }
}
