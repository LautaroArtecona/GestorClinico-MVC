using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
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

        public IActionResult Paciente()
        {
            return View();
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

        public IActionResult Administrativo()
        {
            return View();
        }
    }
}