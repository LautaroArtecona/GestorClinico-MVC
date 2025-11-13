using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
using WebApplication_GestorClinico.Models;

namespace WebApplication_GestorClinico.Controllers
{
    public class TurnoController : Controller
    {
        private readonly ClinicaDBContext _context;

        public TurnoController(ClinicaDBContext context)
        {
            _context = context;
        }

        // GET: Turno
        public async Task<IActionResult> Index()
        {
            var clinicaDBContext = _context.Turnos.Include(t => t.CentroMedico).Include(t => t.Especialidad).Include(t => t.Estado).Include(t => t.Medico).Include(t => t.Paciente);
            return View(await clinicaDBContext.ToListAsync());
        }

        // GET: Turno/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var turno = await _context.Turnos
                .Include(t => t.CentroMedico)
                .Include(t => t.Especialidad)
                .Include(t => t.Estado)
                .Include(t => t.Medico)
                .Include(t => t.Paciente)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (turno == null)
            {
                return NotFound();
            }

            return View(turno);
        }

        // GET: Turno/Create
        public IActionResult Create()
        {
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id");
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id");
            ViewData["EstadoId"] = new SelectList(_context.Estados, "Id", "Id");
            ViewData["MedicoId"] = new SelectList(_context.Medicos, "Id", "Id");
            ViewData["PacienteId"] = new SelectList(_context.Pacientes, "Id", "Id");
            return View();
        }

        // POST: Turno/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FechaHoraInicio,DuracionEnMinutos,EstadoId,Activo,CentroMedicoId,MedicoId,EspecialidadId,PacienteId")] Turno turno)
        {
            if (ModelState.IsValid)
            {
                _context.Add(turno);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id", turno.CentroMedicoId);
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id", turno.EspecialidadId);
            ViewData["EstadoId"] = new SelectList(_context.Estados, "Id", "Id", turno.EstadoId);
            ViewData["MedicoId"] = new SelectList(_context.Medicos, "Id", "Id", turno.MedicoId);
            ViewData["PacienteId"] = new SelectList(_context.Pacientes, "Id", "Id", turno.PacienteId);
            return View(turno);
        }

        // GET: Turno/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null)
            {
                return NotFound();
            }
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id", turno.CentroMedicoId);
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id", turno.EspecialidadId);
            ViewData["EstadoId"] = new SelectList(_context.Estados, "Id", "Id", turno.EstadoId);
            ViewData["MedicoId"] = new SelectList(_context.Medicos, "Id", "Id", turno.MedicoId);
            ViewData["PacienteId"] = new SelectList(_context.Pacientes, "Id", "Id", turno.PacienteId);
            return View(turno);
        }

        // POST: Turno/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FechaHoraInicio,DuracionEnMinutos,EstadoId,Activo,CentroMedicoId,MedicoId,EspecialidadId,PacienteId")] Turno turno)
        {
            if (id != turno.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(turno);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TurnoExists(turno.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id", turno.CentroMedicoId);
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id", turno.EspecialidadId);
            ViewData["EstadoId"] = new SelectList(_context.Estados, "Id", "Id", turno.EstadoId);
            ViewData["MedicoId"] = new SelectList(_context.Medicos, "Id", "Id", turno.MedicoId);
            ViewData["PacienteId"] = new SelectList(_context.Pacientes, "Id", "Id", turno.PacienteId);
            return View(turno);
        }

        // GET: Turno/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var turno = await _context.Turnos
                .Include(t => t.CentroMedico)
                .Include(t => t.Especialidad)
                .Include(t => t.Estado)
                .Include(t => t.Medico)
                .Include(t => t.Paciente)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (turno == null)
            {
                return NotFound();
            }

            return View(turno);
        }

        // POST: Turno/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno != null)
            {
                _context.Turnos.Remove(turno);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TurnoExists(int id)
        {
            return _context.Turnos.Any(e => e.Id == id);
        }
    }
}
