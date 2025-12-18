using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
using WebApplication_GestorClinico.Models;
using WebApplication_GestorClinico.Models.Vistas;

namespace WebApplication_GestorClinico.Controllers
{
    public class GuardiaController : Controller
    {
        private readonly ClinicaDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        // Clave para guardar en sesión
        private const string SESSION_GUARDIA_ID = "GuardiaSeleccionadaId";
        private const string SESSION_GUARDIA_NOMBRE = "GuardiaSeleccionadaNombre";

        public GuardiaController(ClinicaDBContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // SELECCIONAR GUARDIA : guarda en la sesion la guardia seleccionada

        [HttpGet]
        public IActionResult Seleccionar()
        {
            // Cargamos las guardias disponibles
            var guardias = _context.Guardias
                .Include(g => g.CentroMedico)
                .Select(g => new {
                    Id = g.Id,
                    Descripcion = $"{g.Nombre} ({g.CentroMedico.Barrio})"
                })
                .ToList();

            ViewData["Guardias"] = new SelectList(guardias, "Id", "Descripcion");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Seleccionar(int guardiaId)
        {
            var guardia = await _context.Guardias
                .Include(g => g.CentroMedico)
                .FirstOrDefaultAsync(g => g.Id == guardiaId);

            if (guardia == null) return View();

            // Guardamos la elección en la SESIÓN del usuario
            HttpContext.Session.SetInt32(SESSION_GUARDIA_ID, guardia.Id);
            HttpContext.Session.SetString(SESSION_GUARDIA_NOMBRE, $"{guardia.Nombre} - {guardia.CentroMedico.Barrio}");

            if (User.IsInRole("Medico"))
            {
                // Si es médico, lo mandamos a su tablero
                return RedirectToAction(nameof(MedicoIndex));
            }

            // Si es admin (u otro), lo mandamos al tablero de gestión
            return RedirectToAction(nameof(Index));
        }


        // INDEX: Muestra la lista de pacientes esperando

        public async Task<IActionResult> Index()
        {
            // Verificamos si ya eligió guardia
            int? guardiaId = HttpContext.Session.GetInt32(SESSION_GUARDIA_ID);

            if (guardiaId == null)
            {
                // Si no eligió, lo mandamos a elegir
                return RedirectToAction(nameof(Seleccionar));
            }

            // Pasamos el nombre de la guardia a la vista para mostrarlo en el título
            ViewBag.NombreGuardia = HttpContext.Session.GetString(SESSION_GUARDIA_NOMBRE);

            // Filtramos solo por esa guardia
            var pacientesEnEspera = await _context.PacientesEnEspera
                .Include(p => p.Paciente)
                .Include(p => p.Guardia)
                .Include(p => p.Estado)
                .Where(p => p.Estado.Nombre == "En Espera" &&
                            p.GuardiaId == guardiaId.Value) 
                .OrderBy(p => p.HoraDeIngreso)
                .ToListAsync();

            return View(pacientesEnEspera);
        }

        // INGRESO (GET)
        [HttpGet]
        public IActionResult Ingreso()
        {
            // Validamos sesión también aquí
            if (HttpContext.Session.GetInt32(SESSION_GUARDIA_ID) == null)
                return RedirectToAction(nameof(Seleccionar));

            ViewBag.NombreGuardia = HttpContext.Session.GetString(SESSION_GUARDIA_NOMBRE);
            return View();
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

            // Devolvemos la vista CON el paciente para mostrar la tarjeta
            return View("Ingreso", paciente);
        }


        // CONFIRMAR INGRESO (POST): Guarda en la BD

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarIngreso(int idPaciente)
        {
            int? guardiaId = HttpContext.Session.GetInt32(SESSION_GUARDIA_ID);
            if (guardiaId == null) return RedirectToAction(nameof(Seleccionar));

            // Verificar duplicados en ESTA guardia
            bool yaEnCola = await _context.PacientesEnEspera
                .Include(p => p.Estado)
                .AnyAsync(p => p.PacienteId == idPaciente &&
                          p.Estado.Nombre == "En Espera" &&
                          p.GuardiaId == guardiaId.Value); 

            if (yaEnCola)
            {
                TempData["Error"] = "El paciente ya se encuentra en la lista de espera de esta guardia.";
                return RedirectToAction(nameof(Index));
            }

            var estadoEnEspera = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "En Espera");

            var nuevoIngreso = new PacienteEnEspera
            {
                PacienteId = idPaciente,
                GuardiaId = guardiaId.Value,
                EstadoId = estadoEnEspera.Id,
                HoraDeIngreso = DateTime.Now
            };

            _context.PacientesEnEspera.Add(nuevoIngreso);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Paciente ingresado correctamente.";
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


        //  ZONA MÉDICO


        // LISTADO DE GUARDIA (Versión Médico)
        public async Task<IActionResult> MedicoIndex()
        {
            // Validar Sesión (Si no eligió guardia, lo mandamos a elegir)
            int? guardiaId = HttpContext.Session.GetInt32("GuardiaSeleccionadaId");
            if (guardiaId == null) return RedirectToAction(nameof(Seleccionar));

            ViewBag.NombreGuardia = HttpContext.Session.GetString("GuardiaSeleccionadaNombre");

            // Traemos la cola filtrada por la guardia elegida
            var cola = await _context.PacientesEnEspera
                .Include(p => p.Paciente)
                .Include(p => p.Estado)
                .Where(p => p.GuardiaId == guardiaId.Value && p.Estado.Nombre == "En Espera")
                .OrderBy(p => p.HoraDeIngreso)
                .ToListAsync();

            return View(cola);
        }

        // LLAMAR PACIENTE (GET) - Abre la pantalla de consulta
        public async Task<IActionResult> Atender(int idCola)
        {
            var registro = await _context.PacientesEnEspera
                .Include(p => p.Paciente)
                .FirstOrDefaultAsync(p => p.Id == idCola);

            if (registro == null) return RedirectToAction(nameof(MedicoIndex));

            // Preparamos el ViewModel con los datos del paciente
            var modelo = new AtencionGuardia
            {
                IdCola = registro.Id,
                PacienteId = registro.PacienteId,
                NombrePaciente = $"{registro.Paciente.Apellido}, {registro.Paciente.Nombre}",
                Dni = registro.Paciente.Dni,
                ObraSocial = registro.Paciente.ObraSocial
            };

            return View(modelo);
        }

        // FINALIZAR CONSULTA (POST) - Guarda todo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarAtencion(AtencionGuardia model)
        {
            //  Limpieza y Validación
            ModelState.Remove("Ordenes");
            ModelState.Remove("Recetas");

            if (!ModelState.IsValid) return View("Atender", model);

            //  Obtener datos necesarios (Médico y Historia)
            var medico = await ObtenerMedicoActual();
            var historia = await ObtenerOCrearHistoria(model.PacienteId);

            //  Crear y Guardar la Evolución
            var evolucion = new EvolucionMedica
            {
                Fecha = DateTime.Now,
                Diagnostico = model.Diagnostico,
                Tratamiento = model.Tratamiento,
                Observacion = model.Observacion,
                HistoriaClinicaId = historia.Id,
                MedicoId = medico.Id
            };
            _context.EvolucionesMedicas.Add(evolucion);

            //  Procesar Listas Auxiliares (Usando métodos privados)
            GuardarOrdenes(model.Ordenes, historia.Id, model.Diagnostico);
            GuardarRecetas(model.Recetas, historia.Id);

            //  Actualizar la Cola de Espera (Dar salida al paciente)
            await ActualizarEstadoCola(model.IdCola);

            //  Guardar cambios masivos
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Atención finalizada. Se guardaron evolución, estudios y recetas.";
            return RedirectToAction(nameof(MedicoIndex));
        }


        //   MÉTODOS PRIVADOS (LOGICA ENCAPSULADA)

        private async Task<Medico> ObtenerMedicoActual()
        {
            var userIdentity = _userManager.GetUserId(User);
            var medico = await _context.Medicos.FirstOrDefaultAsync(m => m.UsuarioId == userIdentity);

            // Fallback: Si por alguna razón no encuentra al médico, devuelve el primero (para evitar crash)
            return medico ?? await _context.Medicos.FirstOrDefaultAsync();
        }

        private async Task<HistoriaClinica> ObtenerOCrearHistoria(int pacienteId)
        {
            var historia = await _context.HistoriasClinicas
                .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

            if (historia == null)
            {
                historia = new HistoriaClinica { PacienteId = pacienteId };
                _context.Add(historia);
                // Guardamos ya para asegurar que tenga ID generado por la BD
                await _context.SaveChangesAsync();
            }
            return historia;
        }

        private void GuardarOrdenes(List<OrdenMedicaDTO> ordenesDto, int historiaId, string diagnosticoGeneral)
        {
            if (ordenesDto == null || !ordenesDto.Any()) return;

            foreach (var dto in ordenesDto)
            {
                if (!string.IsNullOrEmpty(dto.NombreEstudio))
                {
                    _context.OrdenesMedicas.Add(new OrdenMedica
                    {
                        Fecha = DateTime.Now,
                        NombreEstudio = dto.NombreEstudio,
                        // Si no especificó diagnóstico en el estudio, usa el general de la consulta
                        Diagnostico = dto.Diagnostico ?? diagnosticoGeneral,
                        HistoriaClinicaId = historiaId
                    });
                }
            }
        }

        private void GuardarRecetas(List<RecetaDTO> recetasDto, int historiaId)
        {
            if (recetasDto == null || !recetasDto.Any()) return;

            foreach (var dto in recetasDto)
            {
                if (!string.IsNullOrEmpty(dto.Medicamento))
                {
                    _context.Recetas.Add(new Receta
                    {
                        Fecha = DateTime.Now,
                        Medicamento = dto.Medicamento,
                        Dosis = dto.Dosis,
                        Cantidad = dto.Cantidad,
                        HistoriaClinicaId = historiaId
                    });
                }
            }
        }

        private async Task ActualizarEstadoCola(int idCola)
        {
            var registroCola = await _context.PacientesEnEspera.FindAsync(idCola);
            var estadoAtendido = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Atendido");

            if (registroCola != null && estadoAtendido != null)
            {
                registroCola.EstadoId = estadoAtendido.Id;
                registroCola.HoraAtencion = DateTime.Now; // Marcamos la hora de salida
                _context.Update(registroCola);
            }
        }
    }


}