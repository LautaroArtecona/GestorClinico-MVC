using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
            var pacientes = _context.Pacientes.Include(p => p.Clinica).Include(p => p.Usuario);
            return View(await pacientes.ToListAsync());
        }

        // GET: Paciente/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var paciente = await _context.Pacientes
                .Include(p => p.Clinica)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (paciente == null) return NotFound();

            return View(paciente);
        }

        // GET: Paciente/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Paciente/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ObraSocial,Dni,Nombre,Apellido,Email,Activo")] Paciente paciente)
        {
            // Validar Duplicados y Reactivación
            if (await ExistePacienteDuplicado(paciente))
            {
                return View(paciente);
            }

            // Preparar Modelo
            PrepararDatosModelo(paciente);

            if (ModelState.IsValid)
            {
                // Crear Usuario Identity
                var user = await CrearUsuarioIdentity(paciente);

                if (user != null)
                {
                    // Guardar Paciente
                    paciente.UsuarioId = user.Id;
                    _context.Add(paciente);
                    await _context.SaveChangesAsync();

                    // Crear Historia Clínica Inicial
                    await CrearHistoriaClinicaInicial(paciente.Id);

                    return RedirectToAction(nameof(Index));
                }
            }
            return View(paciente);
        }

        // POST: Paciente/Reactivar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            var paciente = await _context.Pacientes.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (paciente == null) return NotFound();

            // Reactivar BD
            paciente.Activo = true;
            _context.Pacientes.Update(paciente);

            // Desbloquear Usuario
            await DesbloquearUsuario(paciente.UsuarioId);

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Paciente reactivado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Paciente/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound();

            return View(paciente);
        }

        // POST: Paciente/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ObraSocial,Dni,Nombre,Apellido,Email,UsuarioId,ClinicaId,Activo")] Paciente paciente)
        {
            if (id != paciente.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paciente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PacienteExists(paciente.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(paciente);
        }

        // GET: Paciente/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var paciente = await _context.Pacientes
                .Include(p => p.Clinica)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (paciente == null) return NotFound();

            return View(paciente);
        }

        // POST: Paciente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);

            if (paciente != null)
            {
                // Liberar Turnos Futuros
                await LiberarTurnosFuturos(id);

                // Borrado Lógico
                paciente.Activo = false;
                _context.Pacientes.Update(paciente);

                // Bloquear Usuario
                await BloquearUsuario(paciente.UsuarioId);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PacienteExists(int id)
        {
            return _context.Pacientes.Any(e => e.Id == id);
        }


        //   MÉTODOS PRIVADOS (Auxiliares)

        private async Task<bool> ExistePacienteDuplicado(Paciente paciente)
        {
            var existente = await _context.Pacientes
               .IgnoreQueryFilters()
               .FirstOrDefaultAsync(p => p.Dni == paciente.Dni);

            if (existente != null)
            {
                if (existente.Activo)
                {
                    ModelState.AddModelError("Dni", "Ya existe un paciente activo con este DNI.");
                }
                else
                {
                    ViewBag.IdReactivar = existente.Id;
                    ViewBag.NombreReactivar = $"{existente.Apellido}, {existente.Nombre}";
                    ModelState.AddModelError("Dni", "El paciente existe pero está inactivo.");
                }
                return true;
            }
            return false;
        }

        private void PrepararDatosModelo(Paciente paciente)
        {
            var clinica = _context.Clinicas.FirstOrDefault();
            if (clinica != null) paciente.ClinicaId = clinica.Id;

            paciente.Activo = true;

            // Limpieza
            ModelState.Remove("Clinica");
            ModelState.Remove("ClinicaId");
            ModelState.Remove("Activo");
            ModelState.Remove("Usuario");
            ModelState.Remove("UsuarioId");
            ModelState.Remove("HistoriaClinica");
        }

        private async Task<IdentityUser> CrearUsuarioIdentity(Paciente paciente)
        {
            if (!await _roleManager.RoleExistsAsync("Paciente"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Paciente"));
            }

            var user = new IdentityUser
            {
                UserName = paciente.Dni,
                Email = paciente.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, paciente.Dni); // Pass = DNI

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Paciente");
                return user;
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return null;
            }
        }

        private async Task CrearHistoriaClinicaInicial(int pacienteId)
        {
            var nuevaHistoria = new HistoriaClinica
            {
                PacienteId = pacienteId
            };
            _context.Add(nuevaHistoria);
            await _context.SaveChangesAsync();
        }

        private async Task LiberarTurnosFuturos(int pacienteId)
        {
            var estadoLibre = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Libre");

            if (estadoLibre != null)
            {
                var turnosFuturos = await _context.Turnos
                    .Where(t => t.PacienteId == pacienteId && t.FechaHoraInicio > DateTime.Now)
                    .ToListAsync();

                foreach (var turno in turnosFuturos)
                {
                    turno.PacienteId = null;       // Sacamos al paciente
                    turno.EstadoId = estadoLibre.Id; // Lo dejamos disponible para otro
                }
            }
        }

        private async Task BloquearUsuario(string usuarioId)
        {
            if (!string.IsNullOrEmpty(usuarioId))
            {
                var user = await _userManager.FindByIdAsync(usuarioId);
                if (user != null)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }
            }
        }

        private async Task DesbloquearUsuario(string usuarioId)
        {
            if (!string.IsNullOrEmpty(usuarioId))
            {
                var user = await _userManager.FindByIdAsync(usuarioId);
                if (user != null)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }
            }
        }
    }
}