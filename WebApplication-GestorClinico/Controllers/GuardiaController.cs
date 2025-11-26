using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
using WebApplication_GestorClinico.Models;

namespace WebApplication_GestorClinico.Controllers
{
    public class GuardiaController : Controller
    {
        private readonly ClinicaDBContext _context;

        public GuardiaController(ClinicaDBContext context)
        {
            _context = context;
        }

        // INDEX: Muestra la lista de pacientes esperando

        public async Task<IActionResult> Index()
        {
            var pacientesEnEspera = await _context.PacientesEnEspera
                .Include(p => p.Paciente)
                .Include(p => p.Guardia)
                .Include(p => p.Estado)
                .Where(p => p.Estado.Nombre == "En Espera")
                .OrderBy(p => p.HoraDeIngreso) 
                .ToListAsync();

            return View(pacientesEnEspera);
        }


        // INGRESO (GET): Muestra la pantalla vacía

        [HttpGet]
        public IActionResult Ingreso()
        {
            return View(); // Devuelve la vista vacía (sin modelo)
        }


        // BUSCAR PACIENTE (POST): Solo busca y muestra datos

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuscarPaciente(string dniBusqueda)
        {
            // Guardo el DNI en ViewBag para que no se borre del input
            ViewBag.DniIngresado = dniBusqueda;

            // Validar si escribió algo
            if (string.IsNullOrEmpty(dniBusqueda))
            {
                ViewBag.Error = "Debe ingresar un DNI.";
                return View("Ingreso");
            }

            // Buscar al Paciente
            var paciente = await _context.Pacientes
                                .FirstOrDefaultAsync(p => p.Dni == dniBusqueda);

            if (paciente == null)
            {
                // Si no se encontro
                ViewBag.Error = "Paciente no encontrado. Por favor regístrelo primero.";
                return View("Ingreso", null);
            }

            // ENCONTRADO: Devolvemos la vista CON el paciente para mostrar la tarjeta
            return View("Ingreso", paciente);
        }


        // CONFIRMAR INGRESO (POST): Guarda en la BD

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarIngreso(int idPaciente)
        {
            // Verificar si YA está en la cola
            bool yaEnCola = await _context.PacientesEnEspera
                .Include(p => p.Estado)
                .AnyAsync(p => p.PacienteId == idPaciente && p.Estado.Nombre == "En Espera");

            if (yaEnCola)
            {
                TempData["Error"] = "El paciente ya se encuentra en la lista de espera.";
                return RedirectToAction(nameof(Index));
            }

            // Obtener Guardia y Estado
            var guardia = await _context.Guardias.FirstOrDefaultAsync();
            var estadoEnEspera = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "En Espera");

            if (guardia == null || estadoEnEspera == null)
            {
                TempData["Error"] = "Error de configuración: Faltan Guardias o Estados.";
                return RedirectToAction(nameof(Index));
            }

            // Guardar el Ingreso
            var nuevoIngreso = new PacienteEnEspera
            {
                PacienteId = idPaciente,
                GuardiaId = guardia.Id,
                EstadoId = estadoEnEspera.Id,
                HoraDeIngreso = DateTime.Now 
            };

            _context.PacientesEnEspera.Add(nuevoIngreso);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Paciente ingresado a la guardia correctamente.";
            return RedirectToAction(nameof(Index));
        }



        // CANCELAR ATENCIÓN (POST): Cambia estado a 'Cancelado'

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarAtencion(int idCola)
        {
            // Buscamos el registro en la cola
            var registroCola = await _context.PacientesEnEspera.FindAsync(idCola);

            if (registroCola == null)
            {
                TempData["Error"] = "No se encontró el registro.";
                return RedirectToAction(nameof(Index));
            }

            // Buscamos el estado "Cancelado"
            var estadoCancelado = await _context.Estados
                .FirstOrDefaultAsync(e => e.Nombre == "Cancelado");

            if (estadoCancelado == null)
            {
                TempData["Error"] = "Error: El estado 'Cancelado' no existe en la base de datos.";
                return RedirectToAction(nameof(Index));
            }

            // Actualizamos el estado
            registroCola.EstadoId = estadoCancelado.Id;

            _context.PacientesEnEspera.Update(registroCola);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La atención ha sido cancelada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }


}