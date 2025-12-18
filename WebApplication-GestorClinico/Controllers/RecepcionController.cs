using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication_GestorClinico.Context;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Models;

namespace WebApplication_GestorClinico.Controllers
{
    //[Authorize(Roles = "Administrativo")]
    public class RecepcionController : Controller
    {
        private readonly ClinicaDBContext _context;

        public RecepcionController(ClinicaDBContext context)
        {
            _context = context;
        }

        // GET: Vista Principal de Recepción
        public IActionResult Index(int? centroId, string dniPaciente)
        {
            // Cargar Centros Médicos para el desplegable
            ViewBag.Centros = new SelectList(_context.CentrosMedicos.ToList(), "Id", "Barrio", centroId);
            ViewBag.CentroSeleccionado = centroId;
            ViewBag.DniBusqueda = dniPaciente;

            // Si no seleccionó centro ni buscó DNI, devolvemos vista vacía
            if (centroId == null && string.IsNullOrEmpty(dniPaciente))
            {
                return View(new List<Turno>());
            }

            // Buscar Turnos de HOY
            var query = _context.Turnos
                .Include(t => t.Paciente)
                .Include(t => t.Medico)
                .Include(t => t.Especialidad)
                .Include(t => t.Estado)
                .Where(t => t.FechaHoraInicio.Date == DateTime.Today && t.Activo == true);

            // Filtros
            if (centroId.HasValue)
            {
                // Asumiendo que Turno tiene relacion con CentroMedico o a través del Médico
                // Si Turno tiene CentroMedicoId directo:
                query = query.Where(t => t.CentroMedicoId == centroId);
            }

            if (!string.IsNullOrEmpty(dniPaciente))
            {
                query = query.Where(t => t.Paciente.Dni == dniPaciente);
            }

            // Ordenamos por hora
            var turnos = query.OrderBy(t => t.FechaHoraInicio).ToList();

            return View(turnos);
        }

        // POST: Dar Presente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarPresente(int turnoId)
        {
            var turno = await _context.Turnos.FindAsync(turnoId);
            var estadoEnEspera = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "En Espera");

            if (turno != null && estadoEnEspera != null)
            {
                turno.EstadoId = estadoEnEspera.Id;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "¡Presente registrado! El paciente ya aparece en la lista del médico.";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el estado.";
            }

            // Redirigimos conservando el DNI para que no se le borre la pantalla al admin
            // (Necesitamos recuperar el DNI del paciente para reenviarlo al Index)
            var dni = turno?.Paciente?.Dni;
            if (turno?.Paciente == null)
            {
                // Si por lazy loading no vino el paciente, lo buscamos rápido
                var t = await _context.Turnos.Include(x => x.Paciente).FirstOrDefaultAsync(x => x.Id == turnoId);
                dni = t?.Paciente?.Dni;
            }

            return RedirectToAction("Index", new { centroId = turno?.CentroMedicoId, dniPaciente = dni });
        }
    }
}
