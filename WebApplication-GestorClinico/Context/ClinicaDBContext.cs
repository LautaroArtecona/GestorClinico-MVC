using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication_GestorClinico.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace WebApplication_GestorClinico.Context
{
    public class ClinicaDBContext : IdentityDbContext
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
        public DbSet<Estado> Estados { get; set; }

        // Codigo para desambiguar que tome el id de cada clase
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CONFIGURACIÓN DE CLÍNICA (Desambiguación de listas, solucion al error del add-migration)
            modelBuilder.Entity<Clinica>()
                .HasMany(clinica => clinica.Pacientes)
                .WithOne(paciente => paciente.Clinica)
                .HasForeignKey(paciente => paciente.ClinicaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Clinica>()
                .HasMany(clinica => clinica.Medicos)
                .WithOne(medico => medico.Clinica)
                .HasForeignKey(medico => medico.ClinicaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Clinica>()
                .HasMany(clinica => clinica.Administrativos)
                .WithOne(admin => admin.Clinica)
                .HasForeignKey(admin => admin.ClinicaId)
                .OnDelete(DeleteBehavior.Restrict);

            // CONFIGURACIÓN DE TURNOS (Evitar error de ciclos, me daba error al momento del update-database)

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Medico)
                .WithMany(m => m.Turnos)
                .HasForeignKey(t => t.MedicoId)
                .OnDelete(DeleteBehavior.Restrict); // No borra turnos si se borra médico

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.CentroMedico)
                .WithMany(cm => cm.Turnos)
                .HasForeignKey(t => t.CentroMedicoId)
                .OnDelete(DeleteBehavior.Restrict); // No borra turnos si se borra centro

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Especialidad)
                .WithMany()
                .HasForeignKey(t => t.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict); // No borra turnos si se borra especialidad

            modelBuilder.Entity<Turno>()
               .HasOne(t => t.Estado)
               .WithMany()
               .HasForeignKey(t => t.EstadoId)
               .OnDelete(DeleteBehavior.Restrict);

            // Configuración para evitar ciclos en EvolucionMedica -> Medico
            modelBuilder.Entity<EvolucionMedica>()
                .HasOne(e => e.Medico)
                .WithMany() 
                .HasForeignKey(e => e.MedicoId)
                .OnDelete(DeleteBehavior.Restrict);

            // FILTROS GLOBALES DE BORRADO LÓGICO (Activo = true)
            // EF aplicará "WHERE Activo = 1" automáticamente en todas las consultas

            // Personas
            modelBuilder.Entity<Paciente>().HasQueryFilter(p => p.Activo);
            modelBuilder.Entity<Medico>().HasQueryFilter(m => m.Activo);
            modelBuilder.Entity<Administrativo>().HasQueryFilter(a => a.Activo);

            // Otras Entidades
            modelBuilder.Entity<Turno>().HasQueryFilter(t => t.Activo);
            modelBuilder.Entity<CentroMedico>().HasQueryFilter(c => c.Activo);
            modelBuilder.Entity<Especialidad>().HasQueryFilter(e => e.Activo);

        }

    }

}
