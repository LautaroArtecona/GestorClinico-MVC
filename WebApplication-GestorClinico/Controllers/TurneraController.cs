using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
using WebApplication_GestorClinico.Models;

namespace WebApplication_GestorClinico.Controllers
{
    public class TurneraController : Controller
    {
        private readonly ClinicaDBContext _context;

        public TurneraController(ClinicaDBContext context)
        {
            _context = context;
        }


        // PANTALLA INICIAL (GET)

        public IActionResult Index()
        {
            // Cargamos las listas para los filtros (Médicos y Especialidades)
            CargarListasDesplegables();
            return View();
        }


        // BUSCAR PACIENTE

        [HttpPost]
        public async Task<IActionResult> BuscarPaciente(string dniBusqueda)
        {
            CargarListasDesplegables(); // Recargamos listas siempre

            if (string.IsNullOrEmpty(dniBusqueda))
            {
                ViewBag.Error = "Debe ingresar un DNI.";
                return View("Index");
            }

            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Dni == dniBusqueda);

            if (paciente == null)
            {
                ViewBag.Error = "Paciente no encontrado.";
                ViewBag.DniIngresado = dniBusqueda; // Para que quede guardado y no haya que volver a escribirlo
                return View("Index");
            }

            // Buscamos sus turnos asignados a futuro llamando el metodo privado
            ViewBag.TurnosAsignados = await ObtenerTurnosAsignados(paciente.Id);

            // paciente encontrado y lo pasamos a la vista
            return View("Index", paciente);
        }


        // BUSCAR TURNOS DISPONIBLES

        [HttpPost]
        public async Task<IActionResult> BuscarTurnos(int pacienteId, int? medicoId, int? especialidadId)
        {
            CargarListasDesplegables();

            // Recupero al paciente para mantener su info en pantalla
            var paciente = await _context.Pacientes.FindAsync(pacienteId);
            if (paciente == null) return RedirectToAction(nameof(Index));

            // Consulta de Turnos
            var query = _context.Turnos
                .Include(t => t.Medico)
                .Include(t => t.Especialidad)
                .Include(t => t.CentroMedico)
                .Include(t => t.Estado)
                .Where(t => t.Activo == true &&                 // Que no esté borrado
                            t.Estado.Nombre == "Libre" &&       // Que esté LIBRE
                            t.FechaHoraInicio > DateTime.Now);  // Que sea a futuro

            // Aplicamos filtros si el usuario seleccionó algo
            if (medicoId.HasValue)
            {
                query = query.Where(t => t.MedicoId == medicoId.Value);
            }
            if (especialidadId.HasValue)
            {
                query = query.Where(t => t.EspecialidadId == especialidadId.Value);
            }

            var turnosEncontrados = await query.OrderBy(t => t.FechaHoraInicio).ToListAsync();

            // Pasamos datos a la vista
            ViewBag.Turnos = turnosEncontrados; // La lista de resultados
            ViewBag.FiltroMedico = medicoId;    // Para recordar qué filtró
            ViewBag.FiltroEspecialidad = especialidadId;

            ViewBag.TurnosAsignados = await ObtenerTurnosAsignados(paciente.Id);

            return View("Index", paciente); // Devolvemos la vista con el paciente cargado
        }


        // RESERVAR (Confirmar)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservarTurno(int turnoId, int pacienteId)
        {
            var turno = await _context.Turnos.FindAsync(turnoId);
            var paciente = await _context.Pacientes.FindAsync(pacienteId);

            if (turno == null || paciente == null)
            {
                TempData["Error"] = "Ocurrió un error al procesar la solicitud.";
                return RedirectToAction(nameof(Index));
            }

            // Validar estado
            var estadoOtorgado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Asignado");
            if (estadoOtorgado == null)
            {
                TempData["Error"] = "Error de configuración: Falta estado 'Otorgado'.";
                return RedirectToAction(nameof(Index));
            }

            // Asignar
            turno.PacienteId = pacienteId;
            turno.EstadoId = estadoOtorgado.Id;

            _context.Update(turno);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Turno reservado con éxito para {paciente.Apellido}, {paciente.Nombre}.";
            return RedirectToAction(nameof(Index));
        }

 
        // CANCELAR TURNO (Liberarlo)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarTurno(int turnoId, int pacienteId)
        {
            // Buscamos el turno
            var turno = await _context.Turnos.FindAsync(turnoId);
            if (turno == null) return RedirectToAction(nameof(Index));

            // Buscamos el estado "Libre"
            var estadoLibre = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Libre");
            if (estadoLibre == null)
            {
                TempData["Error"] = "Error de configuración: No existe el estado 'Libre'.";
                return RedirectToAction(nameof(Index));
            }

            // LIBERAMOS EL TURNO
            turno.PacienteId = null;       // Sacamos al paciente del turno
            turno.EstadoId = estadoLibre.Id; // Lo ponemos disponible de nuevo

            _context.Update(turno);
            await _context.SaveChangesAsync();

            // RECARGAMOS LA VISTA MANTENIENDO AL PACIENTE para que no haya que ingresarlo de nuevo

            var paciente = await _context.Pacientes.FindAsync(pacienteId);
            TempData["Mensaje"] = "El turno ha sido cancelado y liberado correctamente.";

            // Recargamos las listas necesarias para la vista
            CargarListasDesplegables();
            ViewBag.TurnosAsignados = await ObtenerTurnosAsignados(pacienteId); // metodo privado

            return View("Index", paciente);
        }

        // Helper Privado
        private void CargarListasDesplegables()
        {
            // Especialidades
            ViewData["Especialidades"] = new SelectList(_context.Especialidades.Where(e => e.Activo), "Id", "Nombre");

            // Médicos
            var medicos = _context.Medicos
                .Where(m => m.Activo)
                .Select(m => new { Id = m.Id, NombreCompleto = m.Apellido + ", " + m.Nombre })
                .OrderBy(m => m.NombreCompleto)
                .ToList();

            ViewData["Medicos"] = new SelectList(medicos, "Id", "NombreCompleto");
        }


        // MEtodo privado para obtener los turnos tomados por el paciente
        private async Task<List<Turno>> ObtenerTurnosAsignados(int pacienteId)
        {
            return await _context.Turnos
                .Include(t => t.Medico)
                .Include(t => t.Especialidad)
                .Include(t => t.Estado)
                .Where(t => t.PacienteId == pacienteId &&
                            t.Activo == true &&
                            t.FechaHoraInicio >= DateTime.Today &&
                            t.Estado.Nombre == "Asignado")
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync();
        }
    }
}