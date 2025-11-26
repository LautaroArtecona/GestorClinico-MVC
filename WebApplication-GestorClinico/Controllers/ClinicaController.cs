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
    public class ClinicaController : Controller
    {
        private readonly ClinicaDBContext _context;

        public ClinicaController(ClinicaDBContext context)
        {
            _context = context;
        }

        // GET: Clinica
        public async Task<IActionResult> Index()
        {
            return View(await _context.Clinicas.ToListAsync());
        }

        // GET: Clinica/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clinica = await _context.Clinicas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clinica == null)
            {
                return NotFound();
            }

            return View(clinica);
        }

        // GET: Clinica/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clinica/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre")] Clinica clinica)
        {
            if (ModelState.IsValid)
            {
                _context.Add(clinica);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(clinica);
        }

        // GET: Clinica/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clinica = await _context.Clinicas.FindAsync(id);
            if (clinica == null)
            {
                return NotFound();
            }
            return View(clinica);
        }

        // POST: Clinica/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre")] Clinica clinica)
        {
            if (id != clinica.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(clinica);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClinicaExists(clinica.Id))
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
            return View(clinica);
        }

        // GET: Clinica/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clinica = await _context.Clinicas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clinica == null)
            {
                return NotFound();
            }

            return View(clinica);
        }

        // POST: Clinica/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clinica = await _context.Clinicas.FindAsync(id);
            if (clinica != null)
            {
                _context.Clinicas.Remove(clinica);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClinicaExists(int id)
        {
            return _context.Clinicas.Any(e => e.Id == id);
        }
    }
}
