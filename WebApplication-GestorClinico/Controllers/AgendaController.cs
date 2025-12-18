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
            //  .Select() para crear una lista temporal con un campo nuevo "NombreCompleto" para no mostrar solo el apellido del medico
            var listaMedicos = _context.Medicos
                .Select(m => new
                {
                    Id = m.Id,
                    NombreCompleto = m.Apellido + ", " + m.Nombre 
                })
                .OrderBy(m => m.NombreCompleto) 
                .ToList();

            // Cargamos las listas para los Dropdowns
            ViewData["MedicoId"] = new SelectList(listaMedicos, "Id", "NombreCompleto"); 
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Barrio"); 

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
                // Recargar listas si hay error
                var listaMedicos = _context.Medicos
                    .Select(m => new { Id = m.Id, NombreCompleto = m.Apellido + ", " + m.Nombre })
                    .OrderBy(m => m.NombreCompleto)
                    .ToList();

                ViewData["MedicoId"] = new SelectList(listaMedicos, "Id", "NombreCompleto", modelo.MedicoId);

                ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Barrio", modelo.CentroMedicoId);
                return View("Gestionar", modelo);
            }

            // Obtener datos necesarios (Medico para saber su especialidad, Estado Libre)
            var medico = await _context.Medicos.FindAsync(modelo.MedicoId);

            var estadoLibre = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Libre");

            // toma posible error
            if (estadoLibre == null)
            {
                TempData["Error"] = "Error: No existe el estado 'Libre' en el sistema. Créelo primero.";
                return RedirectToAction(nameof(Gestionar));
            }

            // LÓGICA DE GENERACIÓN en bucle
            List<Turno> turnosGenerados = new List<Turno>();

            // se itera día por día desde Inicio hasta Fin
            for (DateTime dia = modelo.FechaDesde; dia <= modelo.FechaHasta; dia = dia.AddDays(1))
            {
                // Verificamos si el día actual (Lunes, Martes...) está en la lista elegida
                // (DayOfWeek devuelve 0 para Domingo, 1 Lunes, etc.)
                if (modelo.DiasSeleccionados.Contains((int)dia.DayOfWeek))
                {
                    // Si es un día elegido, generamos los horarios
                    DateTime horaActual = dia.Date + modelo.HoraInicio;
                    DateTime horaLimite = dia.Date + modelo.HoraFin;

                    while (horaActual < horaLimite)
                    {
                        // Crear el Turno
                        var nuevoTurno = new Turno
                        {
                            FechaHoraInicio = horaActual,
                            DuracionEnMinutos = modelo.DuracionMinutos,
                            EstadoId = estadoLibre.Id,
                            MedicoId = modelo.MedicoId,
                            CentroMedicoId = modelo.CentroMedicoId,
                            EspecialidadId = medico.EspecialidadId, // Hereda la especialidad del médico
                            PacienteId = null, // Nace libre
                            Activo = true
                        };

                        turnosGenerados.Add(nuevoTurno);

                        // Avanzamos el horario para el nuevo turno
                        horaActual = horaActual.AddMinutes(modelo.DuracionMinutos);
                    }
                }
            }

            // Guardado Masivo
            if (turnosGenerados.Count > 0)
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

            // Buscar Turno y Historia Clínica
            var turno = await _context.Turnos.FindAsync(model.TurnoId);

            var historiaClinica = await _context.HistoriasClinicas
                .FirstOrDefaultAsync(h => h.PacienteId == model.PacienteId);

            // Si no tiene historia, la creamos
            if (historiaClinica == null)
            {
                // CORREGIDO: Quitamos FechaCreacion para evitar el error
                historiaClinica = new HistoriaClinica
                {
                    PacienteId = model.PacienteId
                };

                _context.Add(historiaClinica);
                await _context.SaveChangesAsync();
            }

            // Guardar Evolución
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

            // Guardar Recetas y Ordenes
            // GUARDAR ÓRDENES MÉDICAS 
            if (model.Ordenes != null && model.Ordenes.Any())
            {
                foreach (var ordenDto in model.Ordenes)
                {
                    // Se guardo solo si hay algo escrito
                    if (!string.IsNullOrEmpty(ordenDto.NombreEstudio))
                    {
                        var orden = new OrdenMedica
                        {
                            Fecha = DateTime.Now,
                            NombreEstudio = ordenDto.NombreEstudio,
                            Diagnostico = ordenDto.Diagnostico ?? model.Diagnostico, // Si no se pone nada, usa el general
                            HistoriaClinicaId = historiaClinica.Id
                        };
                        _context.OrdenesMedicas.Add(orden);
                    }
                }
            }

            //  GUARDAR RECETAS
            if (model.Recetas != null && model.Recetas.Any())
            {
                foreach (var recetaDto in model.Recetas)
                {
                    if (!string.IsNullOrEmpty(recetaDto.Medicamento))
                    {
                        var receta = new Receta
                        {
                            Fecha = DateTime.Now,
                            Medicamento = recetaDto.Medicamento,
                            Dosis = recetaDto.Dosis,
                            Cantidad = recetaDto.Cantidad,
                            HistoriaClinicaId = historiaClinica.Id
                        };
                        _context.Recetas.Add(receta);
                    }
                }
            }

            // ACTUALIZAR ESTADO DEL TURNO A "ATENDIDO"
            var estadoAtendido = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Atendido");
            if (turno != null && estadoAtendido != null)
            {
                turno.EstadoId = estadoAtendido.Id;
                _context.Update(turno);
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Consulta finalizada correctamente.";
            return RedirectToAction(nameof(AtenderConsultorio));
        }
    }
}
