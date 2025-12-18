using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
using WebApplication_GestorClinico.Models;
using WebApplication_GestorClinico.Models.Vistas;

namespace WebApplication_GestorClinico.Controllers
{
    // [Authorize] 
    public class PortalController : Controller
    {
        private readonly ClinicaDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PortalController(ClinicaDBContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // AREA PACIENTE


        public async Task<IActionResult> Paciente()
        {
            var paciente = await ObtenerPacienteActual();
            if (paciente == null) return RedirectToAction("Index", "Home");

            var modelo = new PacienteDashboard
            {
                NombreCompleto = $"{paciente.Nombre} {paciente.Apellido}",
                ProximoTurno = await ObtenerProximoTurnoPaciente(paciente.Id),
                TurnosPendientes = await ContarTurnosFuturos(paciente.Id)
            };

            return View(modelo);
        }

        public async Task<IActionResult> MisTurnos(int? especialidadId, int? medicoId)
        {
            var paciente = await ObtenerPacienteActual();
            if (paciente == null) return RedirectToAction("Index", "Home");

            // Cargar Turnos Reservados
            ViewBag.MisTurnos = await ObtenerTurnosReservados(paciente.Id);

            // Cargar Filtros y Buscador
            CargarFiltrosBuscador(especialidadId, medicoId);

            // Buscar Disponibles (si hay filtros)
            ViewBag.TurnosDisponibles = await BuscarTurnosLibres(especialidadId, medicoId);
            ViewBag.BusquedaRealizada = (especialidadId.HasValue || medicoId.HasValue);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservarTurnoPaciente(int turnoId)
        {
            var paciente = await ObtenerPacienteActual();
            var turno = await _context.Turnos.FindAsync(turnoId);
            var estadoAsignado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Asignado");

            if (paciente != null && turno != null && estadoAsignado != null)
            {
                turno.PacienteId = paciente.Id;
                turno.EstadoId = estadoAsignado.Id;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "¡Turno reservado con éxito!";
            }
            return RedirectToAction(nameof(MisTurnos));
        }

        public async Task<IActionResult> MisEstudios()
        {
            var paciente = await ObtenerPacienteActual();
            if (paciente == null) return RedirectToAction("Index", "Home");

            var ordenes = await ObtenerOrdenesDeHistoria(paciente.Id);
            return View(ordenes);
        }

        public async Task<IActionResult> MisRecetas()
        {
            var paciente = await ObtenerPacienteActual();
            if (paciente == null) return RedirectToAction("Index", "Home");

            var recetas = await ObtenerRecetasDeHistoria(paciente.Id);
            return View(recetas);
        }


        // MEDICO


        public async Task<IActionResult> Medico()
        {
            var medico = await ObtenerMedicoActual();
            if (medico == null) return RedirectToAction("Index", "Home");

            var modelo = new MedicoDashboard
            {
                NombreMedico = $"{medico.Apellido}, {medico.Nombre}",
                TieneAgenda = false
            };

            // Calcular agenda del día (si existe)
            var proximoTurno = await ObtenerProximoTurnoMedico(medico.Id);

            if (proximoTurno != null)
            {
                await CargarEstadisticasMedico(modelo, medico.Id, proximoTurno.FechaHoraInicio.Date);
            }

            return View(modelo);
        }


        // ADMINISTRATIVO


        public async Task<IActionResult> Administrativo()
        {
            var modelo = new AdminDashboard
            {
                CantidadMedicos = await _context.Medicos.CountAsync(),
                CantidadAdministrativos = await _context.Administrativos.CountAsync(),
                CantidadPacientes = await _context.Pacientes.CountAsync(),
                EstadisticasPorCentro = await GenerarEstadisticasCentros()
            };

            return View(modelo);
        }



        //   MÉTODOS PRIVADOS (AYUDANTES)

        // Auxiliares Paciente

        private async Task<Paciente> ObtenerPacienteActual()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == user.Id);
        }

        private async Task<Turno> ObtenerProximoTurnoPaciente(int pacienteId)
        {
            return await _context.Turnos
                .Include(t => t.Medico)
                .Include(t => t.Especialidad)
                .Include(t => t.CentroMedico)
                .Include(t => t.Estado)
                .Where(t => t.PacienteId == pacienteId &&
                            t.Activo == true &&
                            t.FechaHoraInicio > DateTime.Now &&
                            t.Estado.Nombre == "Asignado")
                .OrderBy(t => t.FechaHoraInicio)
                .FirstOrDefaultAsync();
        }

        private async Task<int> ContarTurnosFuturos(int pacienteId)
        {
            return await _context.Turnos
                .Where(t => t.PacienteId == pacienteId &&
                            t.Activo == true &&
                            t.FechaHoraInicio > DateTime.Now &&
                            t.Estado.Nombre == "Asignado")
                .CountAsync();
        }

        private async Task<List<Turno>> ObtenerTurnosReservados(int pacienteId)
        {
            return await _context.Turnos
                .Include(t => t.Medico)
                .Include(t => t.Especialidad)
                .Include(t => t.Estado)
                .Where(t => t.PacienteId == pacienteId &&
                            t.Activo == true &&
                            t.FechaHoraInicio >= DateTime.Now &&
                            t.Estado.Nombre == "Asignado")
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync();
        }

        private void CargarFiltrosBuscador(int? especialidadId, int? medicoId)
        {
            ViewBag.Especialidades = new SelectList(_context.Especialidades.Where(e => e.Activo), "Id", "Nombre", especialidadId);

            var medicos = _context.Medicos.Where(m => m.Activo)
                .Select(m => new { Id = m.Id, Nombre = m.Apellido + ", " + m.Nombre })
                .OrderBy(m => m.Nombre).ToList();

            ViewBag.Medicos = new SelectList(medicos, "Id", "Nombre", medicoId);
        }

        private async Task<List<Turno>> BuscarTurnosLibres(int? especialidadId, int? medicoId)
        {
            if (!especialidadId.HasValue && !medicoId.HasValue) return new List<Turno>();

            var query = _context.Turnos
                 .Include(t => t.Medico)
                 .Include(t => t.Especialidad)
                 .Include(t => t.Estado)
                 .Where(t => t.Activo == true &&
                             t.Estado.Nombre == "Libre" &&
                             t.FechaHoraInicio > DateTime.Now);

            if (especialidadId.HasValue) query = query.Where(t => t.EspecialidadId == especialidadId);
            if (medicoId.HasValue) query = query.Where(t => t.MedicoId == medicoId);

            return await query.OrderBy(t => t.FechaHoraInicio).Take(20).ToListAsync();
        }

        private async Task<List<OrdenMedica>> ObtenerOrdenesDeHistoria(int pacienteId)
        {
            var historia = await _context.HistoriasClinicas
                .Include(h => h.OrdenesMedicas)
                .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

            return historia?.OrdenesMedicas.OrderByDescending(o => o.Fecha).ToList() ?? new List<OrdenMedica>();
        }

        private async Task<List<Receta>> ObtenerRecetasDeHistoria(int pacienteId)
        {
            var historia = await _context.HistoriasClinicas
                .Include(h => h.Recetas)
                .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

            return historia?.Recetas.OrderByDescending(r => r.Fecha).ToList() ?? new List<Receta>();
        }

        // Auxiliares Medico

        private async Task<Medico> ObtenerMedicoActual()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Medicos.FirstOrDefaultAsync(m => m.UsuarioId == user.Id);
        }

        private async Task<Turno> ObtenerProximoTurnoMedico(int medicoId)
        {
            return await _context.Turnos
               .Where(t => t.MedicoId == medicoId && t.Activo == true && t.FechaHoraInicio.Date >= DateTime.Today)
               .OrderBy(t => t.FechaHoraInicio)
               .FirstOrDefaultAsync();
        }

        private async Task CargarEstadisticasMedico(MedicoDashboard modelo, int medicoId, DateTime fecha)
        {
            var turnosDelDia = await _context.Turnos
                .Include(t => t.Estado)
                .Where(t => t.MedicoId == medicoId &&
                            t.Activo == true &&
                            t.FechaHoraInicio.Date == fecha)
                .ToListAsync();

            if (turnosDelDia.Any())
            {
                modelo.TieneAgenda = true;
                modelo.ProximaFecha = fecha;
                modelo.HorarioInicio = turnosDelDia.Min(t => t.FechaHoraInicio).ToString("HH:mm");

                var ultimoTurno = turnosDelDia.OrderBy(t => t.FechaHoraInicio).Last();
                modelo.HorarioFin = ultimoTurno.FechaHoraInicio.AddMinutes(ultimoTurno.DuracionEnMinutos).ToString("HH:mm");

                modelo.TurnosLibres = turnosDelDia.Count(t => t.Estado.Nombre == "Libre");
                modelo.TurnosAsignados = turnosDelDia.Count(t =>
                    t.Estado.Nombre == "Asignado" ||
                    t.Estado.Nombre == "En Espera" ||
                    t.Estado.Nombre == "Atendido");
            }
        }

        // Auxiliares Admin

        private async Task<List<CentroEstadisticaDTO>> GenerarEstadisticasCentros()
        {
            var estadisticas = new List<CentroEstadisticaDTO>();
            var centros = await _context.CentrosMedicos.Include(c => c.Guardias).ToListAsync();

            foreach (var centro in centros)
            {
                var guardiaIds = centro.Guardias.Select(g => g.Id).ToList();

                // En Espera
                int enEspera = await _context.PacientesEnEspera
                    .Include(p => p.Estado)
                    .CountAsync(p => guardiaIds.Contains(p.GuardiaId) && p.Estado.Nombre == "En Espera");

                // Atendidos Hoy (traemos datos en memoria para calcular promedio)
                var atendidosData = await _context.PacientesEnEspera
                    .Include(p => p.Estado)
                    .Where(p => guardiaIds.Contains(p.GuardiaId) &&
                                p.Estado.Nombre == "Atendido" &&
                                p.HoraAtencion != null &&
                                p.HoraAtencion.Value.Date == DateTime.Today)
                    .Select(p => new { p.HoraDeIngreso, p.HoraAtencion })
                    .ToListAsync();

                // Cálculo de Promedio
                string demoraTexto = "0 min";
                if (atendidosData.Any())
                {
                    double promedio = atendidosData.Average(p => (p.HoraAtencion.Value - p.HoraDeIngreso).TotalMinutes);
                    demoraTexto = $"{Math.Round(promedio)} min";
                }

                estadisticas.Add(new CentroEstadisticaDTO
                {
                    NombreBarrio = centro.Barrio,
                    Direccion = centro.Direccion,
                    PacientesEnEspera = enEspera,
                    AtendidosHoy = atendidosData.Count,
                    DemoraPromedio = demoraTexto
                });
            }
            return estadisticas;
        }

    }
}