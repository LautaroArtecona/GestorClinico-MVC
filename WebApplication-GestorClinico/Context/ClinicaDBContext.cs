using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication_GestorClinico.Models;
using System.Collections.Generic;

namespace WebApplication_GestorClinico.Context
{
    public class ClinicaDBContext : DbContext
    {
        public ClinicaDBContext(DbContextOptions<ClinicaDBContext>options) : base(options)
        {
        }
        // Persona no necesita DBSet porque es una clase abstracta

        // Clase principal
        public DbSet<Clinica> Clinicas { get; set; }

        // Centros de Atencion
        public DbSet<CentroMedico> CentrosMedicos { get; set; }

        //Personas
        public DbSet<Medico> Medicos { get; set; }
        public DbSet<Administrativo> Administrativos { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }

        // Autenticacion y Roles
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }

        // Manejo de guardia
        public DbSet<Guardia> Guardias { get; set; }
        public DbSet<PacienteEnEspera> PacientesEnEspera { get; set; }

        // Turnos
        public DbSet<Turno> Turnos { get; set; }

        // Clases de modificacion medica
        public DbSet<HistoriaClinica> HistoriasClinicas { get; set; }
        public DbSet<OrdenMedica> OrdenesMedicas { get; set; }
        public DbSet<Receta> Recetas { get; set; }
        public DbSet<EvolucionMedica> EvolucionesMedicas { get; set; }

        public DbSet<Especialidad> Especialidades { get; set; }

        // Codigo para desambiguar que tome el id de cada clase
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Clinica>()
                .HasMany(clinica => clinica.Pacientes)
                .WithOne(paciente => paciente.Clinica)
                .HasForeignKey(paciente => paciente.ClinicaId) // Busca la FK en la clase base
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Clinica>()
                .HasMany(clinica => clinica.Medicos)
                .WithOne(medico => medico.Clinica)
                .HasForeignKey(medico => medico.ClinicaId) // Busca la FK en la clase base
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Clinica>()
                .HasMany(clinica => clinica.Administrativos)
                .WithOne(admin => admin.Clinica)
                .HasForeignKey(admin => admin.ClinicaId) // Busca la FK en la clase base
                .OnDelete(DeleteBehavior.Restrict);
        }
        public DbSet<WebApplication_GestorClinico.Models.Especialidad> Especialidad { get; set; } = default!;

    }

}
