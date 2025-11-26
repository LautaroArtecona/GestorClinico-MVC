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
    public class GuardiaConfigController : Controller
    {
        private readonly ClinicaDBContext _context;

        public GuardiaConfigController(ClinicaDBContext context)
        {
            _context = context;
        }

        // GET: GuardiaConfig
        public async Task<IActionResult> Index()
        {
            var clinicaDBContext = _context.Guardias.Include(g => g.CentroMedico);
            return View(await clinicaDBContext.ToListAsync());
        }

        // GET: GuardiaConfig/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var guardia = await _context.Guardias
                .Include(g => g.CentroMedico)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (guardia == null)
            {
                return NotFound();
            }

            return View(guardia);
        }

        // GET: GuardiaConfig/Create
        public IActionResult Create()
        {
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id");
            return View();
        }

        // POST: GuardiaConfig/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,CentroMedicoId")] Guardia guardia)
        {
            if (ModelState.IsValid)
            {
                _context.Add(guardia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id", guardia.CentroMedicoId);
            return View(guardia);
        }

        // GET: GuardiaConfig/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var guardia = await _context.Guardias.FindAsync(id);
            if (guardia == null)
            {
                return NotFound();
            }
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id", guardia.CentroMedicoId);
            return View(guardia);
        }

        // POST: GuardiaConfig/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,CentroMedicoId")] Guardia guardia)
        {
            if (id != guardia.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(guardia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GuardiaExists(guardia.Id))
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
            ViewData["CentroMedicoId"] = new SelectList(_context.CentrosMedicos, "Id", "Id", guardia.CentroMedicoId);
            return View(guardia);
        }

        // GET: GuardiaConfig/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var guardia = await _context.Guardias
                .Include(g => g.CentroMedico)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (guardia == null)
            {
                return NotFound();
            }

            return View(guardia);
        }

        // POST: GuardiaConfig/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var guardia = await _context.Guardias.FindAsync(id);
            if (guardia != null)
            {
                _context.Guardias.Remove(guardia);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GuardiaExists(int id)
        {
            return _context.Guardias.Any(e => e.Id == id);
        }
    }
}
