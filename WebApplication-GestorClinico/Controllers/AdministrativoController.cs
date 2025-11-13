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
    public class AdministrativoController : Controller
    {
        private readonly ClinicaDBContext _context;

        public AdministrativoController(ClinicaDBContext context)
        {
            _context = context;
        }

        // GET: Administrativo
        public async Task<IActionResult> Index()
        {
            var clinicaDBContext = _context.Administrativos.Include(a => a.Clinica).Include(a => a.Usuario);
            return View(await clinicaDBContext.ToListAsync());
        }

        // GET: Administrativo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrativo = await _context.Administrativos
                .Include(a => a.Clinica)
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (administrativo == null)
            {
                return NotFound();
            }

            return View(administrativo);
        }

        // GET: Administrativo/Create
        public IActionResult Create()
        {
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id");
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id");
            return View();
        }

        // POST: Administrativo/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Legajo,Dni,Nombre,Apellido,Email,UsuarioId,ClinicaId,Activo")] Administrativo administrativo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(administrativo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", administrativo.ClinicaId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", administrativo.UsuarioId);
            return View(administrativo);
        }

        // GET: Administrativo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrativo = await _context.Administrativos.FindAsync(id);
            if (administrativo == null)
            {
                return NotFound();
            }
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", administrativo.ClinicaId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", administrativo.UsuarioId);
            return View(administrativo);
        }

        // POST: Administrativo/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Legajo,Dni,Nombre,Apellido,Email,UsuarioId,ClinicaId,Activo")] Administrativo administrativo)
        {
            if (id != administrativo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(administrativo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdministrativoExists(administrativo.Id))
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
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", administrativo.ClinicaId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", administrativo.UsuarioId);
            return View(administrativo);
        }

        // GET: Administrativo/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var administrativo = await _context.Administrativos
                .Include(a => a.Clinica)
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (administrativo == null)
            {
                return NotFound();
            }

            return View(administrativo);
        }

        // POST: Administrativo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var administrativo = await _context.Administrativos.FindAsync(id);
            if (administrativo != null)
            {
                _context.Administrativos.Remove(administrativo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AdministrativoExists(int id)
        {
            return _context.Administrativos.Any(e => e.Id == id);
        }
    }
}
