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
    public class MedicoController : Controller
    {
        private readonly ClinicaDBContext _context;

        public MedicoController(ClinicaDBContext context)
        {
            _context = context;
        }

        // GET: Medico
        public async Task<IActionResult> Index()
        {
            var clinicaDBContext = _context.Medicos.Include(m => m.Clinica).Include(m => m.Especialidad).Include(m => m.Usuario);
            return View(await clinicaDBContext.ToListAsync());
        }

        // GET: Medico/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medico = await _context.Medicos
                .Include(m => m.Clinica)
                .Include(m => m.Especialidad)
                .Include(m => m.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (medico == null)
            {
                return NotFound();
            }

            return View(medico);
        }

        // GET: Medico/Create
        public IActionResult Create()
        {
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id");
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id");
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id");
            return View();
        }

        // POST: Medico/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Matricula,EspecialidadId,Dni,Nombre,Apellido,Email,UsuarioId,ClinicaId,Activo")] Medico medico)
        {
            if (ModelState.IsValid)
            {
                _context.Add(medico);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", medico.ClinicaId);
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id", medico.EspecialidadId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", medico.UsuarioId);
            return View(medico);
        }

        // GET: Medico/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medico = await _context.Medicos.FindAsync(id);
            if (medico == null)
            {
                return NotFound();
            }
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", medico.ClinicaId);
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id", medico.EspecialidadId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", medico.UsuarioId);
            return View(medico);
        }

        // POST: Medico/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Matricula,EspecialidadId,Dni,Nombre,Apellido,Email,UsuarioId,ClinicaId,Activo")] Medico medico)
        {
            if (id != medico.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medico);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicoExists(medico.Id))
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
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", medico.ClinicaId);
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Id", medico.EspecialidadId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", medico.UsuarioId);
            return View(medico);
        }

        // GET: Medico/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medico = await _context.Medicos
                .Include(m => m.Clinica)
                .Include(m => m.Especialidad)
                .Include(m => m.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (medico == null)
            {
                return NotFound();
            }

            return View(medico);
        }

        // POST: Medico/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medico = await _context.Medicos.FindAsync(id);
            if (medico != null)
            {
                _context.Medicos.Remove(medico);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MedicoExists(int id)
        {
            return _context.Medicos.Any(e => e.Id == id);
        }
    }
}
