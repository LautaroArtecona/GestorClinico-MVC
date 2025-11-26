using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication_GestorClinico.Context;
using WebApplication_GestorClinico.Models;

namespace WebApplication_GestorClinico.Controllers
{
    public class AdministrativoController : Controller
    {
        private readonly ClinicaDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdministrativoController(ClinicaDBContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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
            return View();
        }

        // POST: Administrativo/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Legajo,Dni,Nombre,Apellido,Email,Activo")] Administrativo administrativo)
        {
            // Los numeros de legajo arrancan en 100000 y se asignan automaticamente
            // busca y si no hay nadie le asigna 100000, si hay alguien le asigna el ultimo + 1
            if (!_context.Administrativos.Any())
            {
                administrativo.Legajo = 100000; // El primero
            }
            else
            {
                int ultimoLegajo = _context.Administrativos.Max(a => a.Legajo);
                administrativo.Legajo = ultimoLegajo + 1;
            }

            // Asignar Clínica y Activo
            var clinica = _context.Clinicas.FirstOrDefault();
            if (clinica != null)
            {
                administrativo.ClinicaId = clinica.Id;
            }
            administrativo.Activo = true;

            // LIMPIEZA DE VALIDACIONES
            ModelState.Remove("Clinica");
            ModelState.Remove("ClinicaId");
            ModelState.Remove("Activo");
            ModelState.Remove("Usuario");
            ModelState.Remove("UsuarioId");
            ModelState.Remove("Legajo");


            // Crea y asigna un usuario Identity al paciente

            if (ModelState.IsValid)
            {
                if (!await _roleManager.RoleExistsAsync("Administrativo"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Administrativo"));
                }

                var user = new IdentityUser
                {
                    UserName = administrativo.Legajo.ToString(), // Usuario = Legajo (convertido a string)
                    Email = administrativo.Email,
                    EmailConfirmed = true
                };

                // Contraseña = Legajo
                var result = await _userManager.CreateAsync(user, administrativo.Legajo.ToString());

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Administrativo");

                    administrativo.UsuarioId = user.Id; // Vinculación

                    _context.Add(administrativo);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
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
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Id", administrativo.UsuarioId);
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
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Id", administrativo.UsuarioId);
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
