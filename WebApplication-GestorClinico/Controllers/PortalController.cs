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

        // PORTAL PACIENTE
        public async Task<IActionResult> Paciente()
        {
            // Identificar Paciente
            var user = await _userManager.GetUserAsync(User);
            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == user.Id);

            if (paciente == null) return RedirectToAction("Index", "Home");

            // Buscar Próximo Turno (El más cercano a la fecha actual)
            var proximoTurno = await _context.Turnos
                .Include(t => t.Medico)
                .Include(t => t.Especialidad)
                .Include(t => t.CentroMedico)
                .Include(t => t.Estado)
                .Where(t => t.PacienteId == paciente.Id &&
                            t.Activo == true &&
                            t.FechaHoraInicio > DateTime.Now &&
                            t.Estado.Nombre == "Asignado") 
                .OrderBy(t => t.FechaHoraInicio)
                .FirstOrDefaultAsync();

            //  Contar cuántos tiene en total a futuro
            int cantidadTurnos = await _context.Turnos
                .Where(t => t.PacienteId == paciente.Id &&
                            t.Activo == true &&
                            t.FechaHoraInicio > DateTime.Now &&
                            t.Estado.Nombre == "Asignado")
                .CountAsync();

            //  Armar Modelo
            var modelo = new PacienteDashboard
            {
                NombreCompleto = $"{paciente.Nombre} {paciente.Apellido}",
                TurnosPendientes = cantidadTurnos,
                ProximoTurno = proximoTurno
            };

            return View(modelo);
        }

        // Mis Turnos (Listado y buscador )
        public async Task<IActionResult> MisTurnos(int? especialidadId, int? medicoId)
        {
            // Identificar Paciente Logueado
            var user = await _userManager.GetUserAsync(User);
            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == user.Id);

            if (paciente == null) return RedirectToAction("Index", "Home");

            // Obtener Turnos ya reservados (futuros)
            var misTurnos = await _context.Turnos
                .Include(t => t.Medico)
                .Include(t => t.Especialidad)
                .Include(t => t.Estado)
                .Where(t => t.PacienteId == paciente.Id &&
                            t.Activo == true &&
                            t.FechaHoraInicio >= DateTime.Now &&
                            t.Estado.Nombre == "Asignado")
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync();

            ViewBag.MisTurnos = misTurnos;

            // Lógica del Buscador (Turnos Libres)
            ViewBag.Especialidades = new SelectList(_context.Especialidades.Where(e => e.Activo), "Id", "Nombre", especialidadId);

            var medicos = _context.Medicos
                .Where(m => m.Activo)
                .Select(m => new { Id = m.Id, Nombre = m.Apellido + ", " + m.Nombre })
                .OrderBy(m => m.Nombre)
                .ToList();
            ViewBag.Medicos = new SelectList(medicos, "Id", "Nombre", medicoId);

            // Si hay filtros, buscamos turnos libres
            List<Turno> turnosDisponibles = new List<Turno>();

            if (especialidadId.HasValue || medicoId.HasValue)
            {
                var query = _context.Turnos
                    .Include(t => t.Medico)
                    .Include(t => t.Especialidad)
                    .Include(t => t.Estado)
                    .Where(t => t.Activo == true &&
                                t.Estado.Nombre == "Libre" &&
                                t.FechaHoraInicio > DateTime.Now);

                if (especialidadId.HasValue) query = query.Where(t => t.EspecialidadId == especialidadId);
                if (medicoId.HasValue) query = query.Where(t => t.MedicoId == medicoId);

                turnosDisponibles = await query.OrderBy(t => t.FechaHoraInicio).Take(20).ToListAsync();
            }

            ViewBag.TurnosDisponibles = turnosDisponibles;
            ViewBag.BusquedaRealizada = (especialidadId.HasValue || medicoId.HasValue);

            return View();
        }

        // Acción para Reservar (Desde el portal del paciente)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservarTurnoPaciente(int turnoId)
        {
            var user = await _userManager.GetUserAsync(User);
            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == user.Id);

            var turno = await _context.Turnos.FindAsync(turnoId);
            var estadoAsignado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Asignado");

            if (turno != null && paciente != null && estadoAsignado != null)
            {
                turno.PacienteId = paciente.Id;
                turno.EstadoId = estadoAsignado.Id;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "¡Turno reservado con éxito!";
            }

            return RedirectToAction(nameof(MisTurnos));
        }

        // Logica de Mis Estudios
        public async Task<IActionResult> MisEstudios()
        {
            var user = await _userManager.GetUserAsync(User);
            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == user.Id);

            if (paciente == null) return RedirectToAction("Index", "Home");

            // Buscamos la Historia Clínica y sus Órdenes
            // operador '?' y '??' para manejar si no tiene historia clinica
            var historia = await _context.HistoriasClinicas
                .Include(h => h.OrdenesMedicas)
                .FirstOrDefaultAsync(h => h.PacienteId == paciente.Id);

            // Si tiene historia, tomamos las órdenes y las ordenamos por fecha (más nuevas primero)
            // Si no tiene historia, devolvemos una lista vacía.
            var ordenes = historia?.OrdenesMedicas
                .OrderByDescending(o => o.Fecha)
                .ToList() ?? new List<OrdenMedica>();

            return View(ordenes);
        }

        // Logica para Mis Recetas
        public async Task<IActionResult> MisRecetas()
        {
            var user = await _userManager.GetUserAsync(User);
            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == user.Id);

            if (paciente == null) return RedirectToAction("Index", "Home");

            var historia = await _context.HistoriasClinicas
                .Include(h => h.Recetas)
                .FirstOrDefaultAsync(h => h.PacienteId == paciente.Id);

            var recetas = historia?.Recetas
                .OrderByDescending(r => r.Fecha)
                .ToList() ?? new List<Receta>();

            return View(recetas);
        }



        // PORTAL MEDICO
        public async Task<IActionResult> Medico()
        {
            // Identificar al Médico Logueado
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Home"); // Seguridad

            var medico = await _context.Medicos.FirstOrDefaultAsync(m => m.UsuarioId == user.Id);
            if (medico == null) return View("Error"); // O manejarlo

            // Busca la proxima fecha con turnos

            var proximoTurno = await _context.Turnos
                .Where(t => t.MedicoId == medico.Id &&
                            t.Activo == true &&
                            t.FechaHoraInicio.Date >= DateTime.Today)
                .OrderBy(t => t.FechaHoraInicio)
                .FirstOrDefaultAsync();

            var modelo = new MedicoDashboard
            {
                NombreMedico = $"{medico.Apellido}, {medico.Nombre}",
                TieneAgenda = false
            };

            // Si encontramos una fecha, calculamos las estadísticas de ese día
            if (proximoTurno != null)
            {
                var fechaAnalizar = proximoTurno.FechaHoraInicio.Date;

                // Traemos todos los turnos de ese día específico
                var turnosDelDia = await _context.Turnos
                    .Include(t => t.Estado)
                    .Where(t => t.MedicoId == medico.Id &&
                                t.Activo == true &&
                                t.FechaHoraInicio.Date == fechaAnalizar)
                    .ToListAsync();

                if (turnosDelDia.Any())
                {
                    modelo.TieneAgenda = true;
                    modelo.ProximaFecha = fechaAnalizar;

                    // Contadores
                    modelo.TurnosLibres = turnosDelDia.Count(t => t.Estado.Nombre == "Libre");
                    modelo.TurnosAsignados = turnosDelDia.Count(t => t.Estado.Nombre == "Asignado"); // O "Otorgado"

                    // Rango Horario (Min y Max)
                    modelo.HorarioInicio = turnosDelDia.Min(t => t.FechaHoraInicio).ToString("HH:mm");
                    modelo.HorarioFin = turnosDelDia.Max(t => t.FechaHoraInicio).AddMinutes(turnosDelDia.First().DuracionEnMinutos).ToString("HH:mm");
                }
            }

            return View(modelo);
        }

        // PORTAL ADMINISTRATIVO

        public async Task<IActionResult> Administrativo()
        {
            // Totales Globales 
            var modelo = new AdminDashboard
            {
                CantidadMedicos = await _context.Medicos.CountAsync(),
                CantidadAdministrativos = await _context.Administrativos.CountAsync(),
                CantidadPacientes = await _context.Pacientes.CountAsync()
            };

            // Obtener todos los Centros Médicos Activos
            var centros = await _context.CentrosMedicos
                .Include(c => c.Guardias) // Traemos sus guardias asociadas
                .ToListAsync();

            // Calcular estadísticas por CADA centro
            foreach (var centro in centros)
            {
                // Obtenemos los IDs de las guardias de este centro (usualmente es 1, pero por si acaso)
                var guardiaIds = centro.Guardias.Select(g => g.Id).ToList();

                // En Espera en este centro
                int enEspera = await _context.PacientesEnEspera
                    .Include(p => p.Estado)
                    .CountAsync(p => guardiaIds.Contains(p.GuardiaId) &&
                                     p.Estado.Nombre == "En Espera");

                // Atendidos HOY en este centro
                var atendidosData = await _context.PacientesEnEspera
                    .Include(p => p.Estado)
                    .Where(p => guardiaIds.Contains(p.GuardiaId) &&
                                p.Estado.Nombre == "Atendido" &&
                                p.HoraAtencion != null &&
                                p.HoraAtencion.Value.Date == DateTime.Today)
                    .Select(p => new { p.HoraDeIngreso, p.HoraAtencion })
                    .ToListAsync();

                int atendidosHoy = atendidosData.Count;
                string demoraTexto = "0 min";

                // Promedio en este centro
                if (atendidosHoy > 0)
                {
                    double promedio = atendidosData
                        .Average(p => (p.HoraAtencion.Value - p.HoraDeIngreso).TotalMinutes);
                    demoraTexto = $"{Math.Round(promedio)} min";
                }

                // Agregamos a la lista
                modelo.EstadisticasPorCentro.Add(new CentroEstadisticaDTO
                {
                    NombreBarrio = centro.Barrio,
                    Direccion = centro.Direccion,
                    PacientesEnEspera = enEspera,
                    AtendidosHoy = atendidosHoy,
                    DemoraPromedio = demoraTexto
                });
            }

            return View(modelo);
        }
    }
}