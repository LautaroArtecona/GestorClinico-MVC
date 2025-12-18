using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
using WebApplication_GestorClinico.Models;
using WebApplication_GestorClinico.Models.Vistas;

namespace WebApplication_GestorClinico.Controllers
{
    //[Authorize(Roles = "Medico")]
    public class AgendaController : Controller
    {
        private readonly ClinicaDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public AgendaController(ClinicaDBContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Agendas/Gestionar
        public IActionResult Gestionar()
        {
            CargarListasDesplegables();

            return View();
        }

        // POST: Agendas/Generar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generar(GeneracionAgenda modelo)
        {
            // Validaciones básicas
            if (modelo.FechaHasta < modelo.FechaDesde)
            {
                ModelState.AddModelError("", "La fecha 'Hasta' no puede ser menor que 'Desde'.");
            }
            if (modelo.DiasSeleccionados == null || !modelo.DiasSeleccionados.Any())
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un día de la semana.");
            }

            if (!ModelState.IsValid)
            {
                CargarListasDesplegables(modelo.MedicoId, modelo.CentroMedicoId);
                return View("Gestionar", modelo);
            }

            // Obtener datos necesarios (Medico para saber su especialidad, Estado Libre)
            var medico = await _context.Medicos.FindAsync(modelo.MedicoId);
            var estadoLibre = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Libre");

            // Llamada al método privado
            var turnosGenerados = CalcularTurnos(modelo, estadoLibre.Id, medico.EspecialidadId);

            // Guardado Masivo
            if (turnosGenerados.Any())
            {
                _context.Turnos.AddRange(turnosGenerados);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Se generaron con éxito {turnosGenerados.Count} turnos.";
            }
            else
            {
                TempData["Error"] = "No se generaron turnos (revise las fechas y días seleccionados).";
            }

            return RedirectToAction(nameof(Gestionar));
        }

        

        // LISTADO DE FECHAS

        public async Task<IActionResult> Cancelar()
        {
            // Identifica al médico logueado
            var userId = _userManager.GetUserId(User);
            var medico = await _context.Medicos.FirstOrDefaultAsync(m => m.UsuarioId == userId);

            if (medico == null) return RedirectToAction("Index", "Home");

            // Buscar turnos FUTUROS que NO estén cancelados
            var turnos = await _context.Turnos
                .Include(t => t.Estado)
                .Include(t => t.Paciente) // Para saber si hay paciente asignado
                .Where(t => t.MedicoId == medico.Id &&
                            t.FechaHoraInicio > DateTime.Now &&
                            t.Estado.Nombre != "Cancelado")
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync();

            // Agrupar por Fecha (Día)
            var modelo = turnos
                .GroupBy(t => t.FechaHoraInicio.Date)
                .Select(grupo => new CancelarAgenda
                {
                    Fecha = grupo.Key,
                    Turnos = grupo.ToList()
                })
                .ToList();

            return View(modelo);
        }


        // CANCELAR DÍA COMPLETO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDia(DateTime fecha)
        {
            var userId = _userManager.GetUserId(User);
            var medico = await _context.Medicos.FirstOrDefaultAsync(m => m.UsuarioId == userId);
            var estadoCancelado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Cancelado");

            // Buscamos los turnos de ESE día para ESE médico
            var turnosDelDia = await _context.Turnos
                .Where(t => t.MedicoId == medico.Id &&
                            t.FechaHoraInicio.Date == fecha.Date &&
                            t.Estado.Nombre != "Cancelado")
                .ToListAsync();

            foreach (var turno in turnosDelDia)
            {
                turno.EstadoId = estadoCancelado.Id;
            }

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se han cancelado {turnosDelDia.Count} turnos del día {fecha.ToShortDateString()}.";

            return RedirectToAction(nameof(Cancelar));
        }


        // CANCELAR TURNO INDIVIDUAL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarTurno(int turnoId)
        {
            var turno = await _context.Turnos.FindAsync(turnoId);
            var estadoCancelado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Cancelado");

            if (turno != null)
            {
                turno.EstadoId = estadoCancelado.Id;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Turno cancelado correctamente.";
            }

            return RedirectToAction(nameof(Cancelar));
        }


        // GET: Atender Consultorio (Lista del día)
        public async Task<IActionResult> AtenderConsultorio()
        {
            var userId = _userManager.GetUserId(User);
            var medico = await _context.Medicos.FirstOrDefaultAsync(m => m.UsuarioId == userId);

            if (medico == null) return RedirectToAction("Index", "Home");

            // Buscamos turnos de HOY para ESTE médico
            var turnosHoy = await _context.Turnos
                .Include(t => t.Paciente)
                .Include(t => t.Estado)
                .Include(t => t.Especialidad)
                .Where(t => t.MedicoId == medico.Id &&
                            t.Activo == true &&
                            t.FechaHoraInicio.Date == DateTime.Today)
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync();

            return View(turnosHoy);
        }


        [HttpGet]
        public async Task<IActionResult> Atender(int turnoId)
        {
            var turno = await _context.Turnos
                .Include(t => t.Paciente)
                .Include(t => t.Medico)
                .Include(t => t.Especialidad) // Agregamos esto por si acaso
                .FirstOrDefaultAsync(t => t.Id == turnoId);

            if (turno == null) return RedirectToAction(nameof(AtenderConsultorio));

            // Reutilizamos el ViewModel de atención
            var model = new WebApplication_GestorClinico.Models.Vistas.AtencionGuardia
            {
                TurnoId = turno.Id,
                IdCola = 0,
                PacienteId = turno.PacienteId ?? 0,
                NombrePaciente = $"{turno.Paciente.Apellido}, {turno.Paciente.Nombre}",
                Dni = turno.Paciente.Dni,
                ObraSocial = turno.Paciente.ObraSocial
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarAtencion(WebApplication_GestorClinico.Models.Vistas.AtencionGuardia model)
        {
            // Limpieza de validaciones
            ModelState.Remove("Ordenes");
            ModelState.Remove("Recetas");

            if (!ModelState.IsValid) return View("Atender", model);

            // Obtener Turno y Médico
            var turno = await _context.Turnos.FindAsync(model.TurnoId);
            if (turno == null) return RedirectToAction(nameof(AtenderConsultorio));

            // Obtener o Crear Historia Clínica 
            var historiaClinica = await ObtenerOCrearHistoria(model.PacienteId);

            // Crear y Guardar la Evolución
            var evolucion = new EvolucionMedica
            {
                HistoriaClinicaId = historiaClinica.Id,
                MedicoId = turno.MedicoId,
                Fecha = DateTime.Now,
                Diagnostico = model.Diagnostico,
                Tratamiento = model.Tratamiento,
                Observacion = model.Observacion
            };
            _context.Add(evolucion);

            // Procesar Listas Auxiliares
            GuardarOrdenes(model.Ordenes, historiaClinica.Id, model.Diagnostico);
            GuardarRecetas(model.Recetas, historiaClinica.Id);

            // Actualizar Estado del Turno
            await ActualizarEstadoTurno(turno);

            // Guardar TODO en una sola transacción
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Consulta finalizada correctamente.";
            return RedirectToAction(nameof(AtenderConsultorio));
        }

        // METODOS AUXILIARES
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

        private async Task ActualizarEstadoTurno(Turno turno)
        {
            var estadoAtendido = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Atendido");
            if (estadoAtendido != null)
            {
                turno.EstadoId = estadoAtendido.Id;
                _context.Update(turno);
            }
        }

        private async Task<HistoriaClinica> ObtenerOCrearHistoria(int pacienteId)
        {
            var historia = await _context.HistoriasClinicas
                .FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

            if (historia == null)
            {
                historia = new HistoriaClinica { PacienteId = pacienteId };
                _context.Add(historia);
                // Guardo aca para asegurar que tenga ID antes de seguir
                await _context.SaveChangesAsync();
            }
            return historia;
        }

        // METODO AUXILIAR PARA CARGAR LISTAS
        private void CargarListasDesplegables(int? medicoIdSeleccionado = null, int? centroIdSeleccionado = null)
        {
            var listaMedicos = _context.Medicos
                .Select(m => new { Id = m.Id, NombreCompleto = m.Apellido + ", " + m.Nombre })
                .OrderBy(m => m.NombreCompleto)
                .ToList();

            ViewData["MedicoId"] = new SelectList(listaMedicos, "Id", "NombreCompleto", medicoIdSeleccionado);
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Barrio", centroIdSeleccionado);
        }

        // METODO AUXILIAR QUE DEVUELVE LISTA DE TURNOS
        private List<Turno> CalcularTurnos(GeneracionAgenda modelo, int estadoLibreId, int especialidadId)
        {
            List<Turno> turnos = new List<Turno>();

            for (DateTime dia = modelo.FechaDesde; dia <= modelo.FechaHasta; dia = dia.AddDays(1))
            {
                if (modelo.DiasSeleccionados.Contains((int)dia.DayOfWeek))
                {
                    DateTime horaActual = dia.Date + modelo.HoraInicio;
                    DateTime horaLimite = dia.Date + modelo.HoraFin;

                    while (horaActual < horaLimite)
                    {
                        turnos.Add(new Turno
                        {
                            FechaHoraInicio = horaActual,
                            DuracionEnMinutos = modelo.DuracionMinutos,
                            EstadoId = estadoLibreId,
                            MedicoId = modelo.MedicoId,
                            CentroMedicoId = modelo.CentroMedicoId,
                            EspecialidadId = especialidadId,
                            PacienteId = null,
                            Activo = true
                        });
                        horaActual = horaActual.AddMinutes(modelo.DuracionMinutos);
                    }
                }
            }
            return turnos;
        }
    }
}
