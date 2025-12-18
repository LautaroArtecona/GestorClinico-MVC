using Microsoft.AspNetCore.Identity;
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
            var medicos = _context.Medicos.Include(m => m.Clinica).Include(m => m.Especialidad).Include(m => m.Usuario);
            return View(await medicos.ToListAsync());
        }

        // GET: Medico/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var medico = await _context.Medicos
                .Include(m => m.Clinica)
                .Include(m => m.Especialidad)
                .Include(m => m.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medico == null) return NotFound();

            return View(medico);
        }

        // GET: Medico/Create
        public IActionResult Create()
        {
            CargarDesplegables();
            return View();
        }

        // POST: Medico/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Matricula,EspecialidadId,Dni,Nombre,Apellido,Email")] Medico medico)
        {
            // Validar Duplicados y Reactivación
            if (await ExisteMedicoDuplicado(medico))
            {
                CargarDesplegables(medico.EspecialidadId);
                return View(medico);
            }

            // Preparar Modelo (Asignar Clínica, Activo, Limpiar ModelState)
            PrepararDatosModelo(medico);

            if (ModelState.IsValid)
            {
                // Crear Usuario y Rol en Identity
                var user = await CrearUsuarioIdentity(medico);

                if (user != null)
                {
                    //  Vincular y Guardar
                    medico.UsuarioId = user.Id;
                    _context.Add(medico);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            CargarDesplegables(medico.EspecialidadId);
            return View(medico);
        }

        // POST: Medico/Reactivar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            var medico = await _context.Medicos.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);
            if (medico == null) return NotFound();

            // Reactivar en BD
            medico.Activo = true;
            _context.Medicos.Update(medico);

            //  Desbloquear Usuario Identity
            await DesbloquearUsuario(medico.UsuarioId);

            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"El médico {medico.Apellido} ha sido reactivado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Medico/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var medico = await _context.Medicos.FindAsync(id);
            if (medico == null) return NotFound();

            CargarDesplegables(medico.EspecialidadId);
            return View(medico);
        }

        // POST: Medico/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Matricula,EspecialidadId,Dni,Nombre,Apellido,Email,UsuarioId,ClinicaId,Activo")] Medico medico)
        {
            if (id != medico.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medico);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicoExists(medico.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            CargarDesplegables(medico.EspecialidadId);
            return View(medico);
        }

        // GET: Medico/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var medico = await _context.Medicos
                .Include(m => m.Clinica)
                .Include(m => m.Especialidad)
                .Include(m => m.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medico == null) return NotFound();

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
                // Cancelar Turnos Futuros
                await CancelarTurnosFuturos(id);

                // Borrado Lógico
                medico.Activo = false;
                _context.Medicos.Update(medico);

                // Bloquear acceso al sistema
                await BloquearUsuario(medico.UsuarioId);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MedicoExists(int id)
        {
            return _context.Medicos.Any(e => e.Id == id);
        }


        //   MÉTODOS AUXILIARES PRIVADOS 

        private void CargarDesplegables(int? especialidadId = null)
        {
            ViewData["EspecialidadId"] = new SelectList(_context.Especialidades, "Id", "Nombre", especialidadId);
        }

        private async Task<bool> ExisteMedicoDuplicado(Medico medico)
        {
            var medicoExistente = await _context.Medicos
               .IgnoreQueryFilters()
               .FirstOrDefaultAsync(m => m.Matricula == medico.Matricula);

            if (medicoExistente != null)
            {
                if (medicoExistente.Activo)
                {
                    ModelState.AddModelError("Matricula", "Ya existe un médico activo con esta matrícula.");
                }
                else
                {
                    // Lógica de Reactivación
                    ViewBag.IdReactivar = medicoExistente.Id;
                    ViewBag.NombreReactivar = $"{medicoExistente.Apellido}, {medicoExistente.Nombre}";
                    ModelState.AddModelError("Matricula", "Este médico ya existe en el sistema pero está inactivo (borrado).");
                }
                return true; // Hay duplicado
            }
            return false; // No hay duplicado
        }

        private void PrepararDatosModelo(Medico medico)
        {
            var clinica = _context.Clinicas.FirstOrDefault();
            if (clinica != null) medico.ClinicaId = clinica.Id;

            medico.Activo = true;

            // Limpiamos lo que no viene del form
            ModelState.Remove("Clinica");
            ModelState.Remove("ClinicaId");
            ModelState.Remove("Activo");
            ModelState.Remove("Usuario");
            ModelState.Remove("UsuarioId");
        }

        private async Task<IdentityUser> CrearUsuarioIdentity(Medico medico)
        {
            // Verificar/Crear Rol
            if (!await _roleManager.RoleExistsAsync("Medico"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Medico"));
            }

            // Crear Objeto Usuario
            var user = new IdentityUser
            {
                UserName = medico.Matricula,
                Email = medico.Email,
                EmailConfirmed = true
            };

            // Guardar en Identity (Pass = Matricula)
            var result = await _userManager.CreateAsync(user, medico.Matricula);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Medico");
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

        private async Task CancelarTurnosFuturos(int medicoId)
        {
            var estadoCancelado = await _context.Estados.FirstOrDefaultAsync(e => e.Nombre == "Cancelado");
            if (estadoCancelado != null)
            {
                var turnosFuturos = await _context.Turnos
                    .Where(t => t.MedicoId == medicoId && t.FechaHoraInicio > DateTime.Now)
                    .ToListAsync();

                foreach (var turno in turnosFuturos)
                {
                    turno.EstadoId = estadoCancelado.Id;
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