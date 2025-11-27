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
    public class MedicoController : Controller
    {
        private readonly ClinicaDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MedicoController(ClinicaDBContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Nombre");

            return View();
        }

        // POST: Medico/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Matricula,EspecialidadId,Dni,Nombre,Apellido,Email")] Medico medico)
        {

            // BUSCAR DUPLICADOS (Incluyendo los borrados)
            var medicoExistente = await _context.Medicos
                .IgnoreQueryFilters() // Mira también los inactivos
                .FirstOrDefaultAsync(m => m.Matricula == medico.Matricula);

            if (medicoExistente != null)
            {
                if (medicoExistente.Activo)
                {
                    //Ya existe y está activo -> Error bloqueante
                    ModelState.AddModelError("Matricula", "Ya existe un médico activo con esta matrícula.");
                }
                else
                {
                    // Existe pero está inactivo -> Ofrecer reactivación
                    // Guardamos el ID del viejo en el ViewBag para usarlo en el botón
                    ViewBag.IdReactivar = medicoExistente.Id;
                    ViewBag.NombreReactivar = $"{medicoExistente.Apellido}, {medicoExistente.Nombre}";

                    ModelState.AddModelError("Matricula", "Este médico ya existe en el sistema pero está inactivo (borrado).");
                }

                // Recargamos lista y devolvemos vista con el error
                ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Nombre", medico.EspecialidadId);
                return View(medico);
            }

            // Buscamos la única clínica del sistema
            var clinica = _context.Clinicas.FirstOrDefault();
            if (clinica != null)
            {
                medico.ClinicaId = clinica.Id;
            }
            medico.Activo = true;

            ModelState.Remove("Clinica");
            ModelState.Remove("ClinicaId");
            ModelState.Remove("Activo");
            ModelState.Remove("Usuario");
            ModelState.Remove("UsuarioId");

            // Creacion de Usuario

            if (ModelState.IsValid)
            {
                // Verificar si el rol existe, sino lo crea
                if (!await _roleManager.RoleExistsAsync("Medico"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Medico"));
                }

                // Crear el Usuario de Identity
                var user = new IdentityUser
                {
                    UserName = medico.Matricula, // Usuario = Matrícula
                    Email = medico.Email,
                    EmailConfirmed = true
                };

                // Creamos el usuario con contraseña = Matrícula
                var result = await _userManager.CreateAsync(user, medico.Matricula);

                if (result.Succeeded)
                {
                    // Asignar Rol
                    await _userManager.AddToRoleAsync(user, "Medico");

                    // Vincular el Médico con el Usuario recién creado
                    medico.UsuarioId = user.Id; // Guardamos el GUID del usuario

                    //  Guardar el Médico en la BD
                    _context.Add(medico);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Si falla (ej. contraseña muy simple o usuario duplicado), mostramos el error
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            // Recargamos la lista de especialidades si falla
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Nombre", medico.EspecialidadId);
            return View(medico);
        }

        // REACTIVAR medicos con borrado logico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            // Buscamos al médico inactivo
            var medico = await _context.Medicos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medico == null) return NotFound();

            // Reactivar Entidad
            medico.Activo = true;
            _context.Medicos.Update(medico);

            // Desbloquear Usuario de Identity
            if (!string.IsNullOrEmpty(medico.UsuarioId))
            {
                var user = await _userManager.FindByIdAsync(medico.UsuarioId);
                if (user != null)
                {
                    // Sacamos el bloqueo del usuario
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"El médico {medico.Apellido} ha sido reactivado exitosamente.";
            return RedirectToAction(nameof(Index));
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
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Id", medico.UsuarioId);
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
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Id", medico.UsuarioId);
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
            // Buscamos el médico
            var medico = await _context.Medicos.FindAsync(id);

            if (medico != null)
            {

                // Buscamos el estado "Cancelado" y cancelamos turnos futuros
                var estadoCancelado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Cancelado");

                if (estadoCancelado != null)
                {
                    var turnosFuturos = await _context.Turnos
                        .Where(t => t.MedicoId == id && t.FechaHoraInicio > DateTime.Now)
                        .ToListAsync();

                    foreach (var turno in turnosFuturos)
                    {
                        turno.EstadoId = estadoCancelado.Id;
                        // turno.Activo = false; para que no se vean mas
                    }
                }

                //BORRADO LÓGICO
                medico.Activo = false; // Lo desactivamos
                _context.Medicos.Update(medico);// Guardamos los cambios (UPDATE, no Remove)

                // BLOQUEO DE USUARIO (NUEVO)
                if (!string.IsNullOrEmpty(medico.UsuarioId))
                {
                    var usuarioIdentity = await _userManager.FindByIdAsync(medico.UsuarioId);
                    if (usuarioIdentity != null)
                    {
                        await _userManager.SetLockoutEnabledAsync(usuarioIdentity, true);
                        await _userManager.SetLockoutEndDateAsync(usuarioIdentity, DateTimeOffset.MaxValue);
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MedicoExists(int id)
        {
            return _context.Medicos.Any(e => e.Id == id);
        }
    }
}
