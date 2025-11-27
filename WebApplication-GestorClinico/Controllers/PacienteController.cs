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
    public class PacienteController : Controller
    {
        private readonly ClinicaDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PacienteController(ClinicaDBContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Paciente
        public async Task<IActionResult> Index()
        {
            var clinicaDBContext = _context.Pacientes.Include(p => p.Clinica).Include(p => p.Usuario);
            return View(await clinicaDBContext.ToListAsync());
        }

        // GET: Paciente/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paciente = await _context.Pacientes
                .Include(p => p.Clinica)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (paciente == null)
            {
                return NotFound();
            }

            return View(paciente);
        }

        // GET: Paciente/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Paciente/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ObraSocial,Dni,Nombre,Apellido,Email,Activo")] Paciente paciente)
        {

            // BUSCAR POR DNI
            var pacienteExistente = await _context.Pacientes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Dni == paciente.Dni);

            if (pacienteExistente != null)
            {
                if (pacienteExistente.Activo)
                {
                    ModelState.AddModelError("Dni", "Ya existe un paciente activo con este DNI.");
                }
                else
                {
                    ViewBag.IdReactivar = pacienteExistente.Id;
                    ViewBag.NombreReactivar = $"{pacienteExistente.Apellido}, {pacienteExistente.Nombre}";
                    ModelState.AddModelError("Dni", "El paciente existe pero está inactivo.");
                }
                return View(paciente);
            }

            // AUTOMATIZACIÓN DE DATOS
            // Asignar Clínica única
            var clinica = _context.Clinicas.FirstOrDefault();
            if (clinica != null)
            {
                paciente.ClinicaId = clinica.Id;
            }

            // comienza en activo (borrado logico)
            paciente.Activo = true;

            // LIMPIEZA DE VALIDACIONES
            ModelState.Remove("Clinica");
            ModelState.Remove("ClinicaId");
            ModelState.Remove("Activo");
            ModelState.Remove("Usuario");
            ModelState.Remove("UsuarioId");
            ModelState.Remove("HistoriaClinica");


            // Crea y asigna un usuario Identity al paciente

            if (ModelState.IsValid)
            {
                if (!await _roleManager.RoleExistsAsync("Paciente"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Paciente"));
                }

                var user = new IdentityUser
                {
                    UserName = paciente.Dni, // Usuario = DNI
                    Email = paciente.Email,
                    EmailConfirmed = true
                };

                // Contraseña = DNI
                var result = await _userManager.CreateAsync(user, paciente.Dni);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Paciente");

                    paciente.UsuarioId = user.Id; // Vinculación

                    _context.Add(paciente);
                    await _context.SaveChangesAsync();

                    // Crea la historia clinica una vez que se registra al paciente

                    var nuevaHistoria = new HistoriaClinica
                    {
                        PacienteId = paciente.Id // Usamos el ID que acabamos de generar
                    };
                    _context.Add(nuevaHistoria);
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
            return View(paciente);
        }

        // REACTIVAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            var paciente = await _context.Pacientes.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (paciente == null) return NotFound();

            // Reactivar
            paciente.Activo = true;
            _context.Pacientes.Update(paciente);

            // Desbloquear Usuario
            if (!string.IsNullOrEmpty(paciente.UsuarioId))
            {
                var user = await _userManager.FindByIdAsync(paciente.UsuarioId);
                if (user != null) await _userManager.SetLockoutEndDateAsync(user, null);
            }

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Paciente reactivado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Paciente/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
            {
                return NotFound();
            }
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", paciente.ClinicaId);
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Id", paciente.UsuarioId);
            return View(paciente);
        }

        // POST: Paciente/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ObraSocial,Dni,Nombre,Apellido,Email,UsuarioId,ClinicaId,Activo")] Paciente paciente)
        {
            if (id != paciente.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paciente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PacienteExists(paciente.Id))
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
            ViewData["ClinicaId"] = new SelectList(_context.Clinicas, "Id", "Id", paciente.ClinicaId);
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Id", paciente.UsuarioId);
            return View(paciente);
        }

        // GET: Paciente/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paciente = await _context.Pacientes
                .Include(p => p.Clinica)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (paciente == null)
            {
                return NotFound();
            }

            return View(paciente);
        }

        // POST: Paciente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Buscamos al paciente
            var paciente = await _context.Pacientes.FindAsync(id);

            if (paciente != null)
            {
                // Buscamos el estado "Libre" para liberar los turnos tomados
                var estadoLibre = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Libre");

                if (estadoLibre != null)
                {
                    // Buscamos solo los turnos A FUTURO que tenga este paciente
                    var turnosFuturos = await _context.Turnos
                        .Where(t => t.PacienteId == id && t.FechaHoraInicio > DateTime.Now)
                        .ToListAsync();

                    foreach (var turno in turnosFuturos)
                    {
                        turno.PacienteId = null;       // Desvinculamos al paciente
                        turno.EstadoId = estadoLibre.Id; // Ponemos el turno disponible para otro
                    }
                }

                // BORRADO LÓGICO DEL PACIENTE
                paciente.Activo = false;
                _context.Pacientes.Update(paciente);

                //  BLOQUEO DEL USUARIO DE IDENTITY 
                if (!string.IsNullOrEmpty(paciente.UsuarioId))
                {
                    var usuarioIdentity = await _userManager.FindByIdAsync(paciente.UsuarioId);

                    if (usuarioIdentity != null)
                    {
                        // Habilitamos el bloqueo
                        await _userManager.SetLockoutEnabledAsync(usuarioIdentity, true);

                        // Le ponemos fecha de fin de bloqueo maxima
                        await _userManager.SetLockoutEndDateAsync(usuarioIdentity, DateTimeOffset.MaxValue);
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PacienteExists(int id)
        {
            return _context.Pacientes.Any(e => e.Id == id);
        }
    }
}
