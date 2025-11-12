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
    public class CentroMedicoController : Controller
    {
        private readonly ClinicaDBContext _context;

        public CentroMedicoController(ClinicaDBContext context)
        {
            _context = context;
        }

        // GET: CentroMedico
        public async Task<IActionResult> Index()
        {
            var clinicaDBContext = _context.CentrosMedicos.Include(c => c.Clinica);
            return View(await clinicaDBContext.ToListAsync());
        }

        // GET: CentroMedico/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var centroMedico = await _context.CentrosMedicos
                .Include(c => c.Clinica)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (centroMedico == null)
            {
                return NotFound();
            }

            return View(centroMedico);
        }

        // GET: CentroMedico/Create
        public IActionResult Create()
        {
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id");
            return View();
        }

        // POST: CentroMedico/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Barrio,Direccion,Telefono,ClinicaId")] CentroMedico centroMedico)
        {
            if (ModelState.IsValid)
            {
                _context.Add(centroMedico);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", centroMedico.ClinicaId);
            return View(centroMedico);
        }

        // GET: CentroMedico/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var centroMedico = await _context.CentrosMedicos.FindAsync(id);
            if (centroMedico == null)
            {
                return NotFound();
            }
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", centroMedico.ClinicaId);
            return View(centroMedico);
        }

        // POST: CentroMedico/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Barrio,Direccion,Telefono,ClinicaId")] CentroMedico centroMedico)
        {
            if (id != centroMedico.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(centroMedico);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CentroMedicoExists(centroMedico.Id))
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
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", centroMedico.ClinicaId);
            return View(centroMedico);
        }

        // GET: CentroMedico/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var centroMedico = await _context.CentrosMedicos
                .Include(c => c.Clinica)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (centroMedico == null)
            {
                return NotFound();
            }

            return View(centroMedico);
        }

        // POST: CentroMedico/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var centroMedico = await _context.CentrosMedicos.FindAsync(id);
            if (centroMedico != null)
            {
                _context.CentrosMedicos.Remove(centroMedico);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CentroMedicoExists(int id)
        {
            return _context.CentrosMedicos.Any(e => e.Id == id);
        }
    }
}
