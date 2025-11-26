using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_GestorClinico.Models.Vistas;


namespace WebApplication_GestorClinico.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        //  VISTAS GET (Para mostrar los 3 diseños diferentes)

        [HttpGet]
        public IActionResult Paciente()
        {
            return View(); //  Views/Account/Paciente.cshtml
        }

        [HttpGet]
        public IActionResult Medico()
        {
            return View(); //  Views/Account/Medico.cshtml
        }

        [HttpGet]
        public IActionResult Administrativo()
        {
            return View(); //  Views/Account/Administrativo.cshtml
        }


        //  LÓGICA DE LOGIN (POST) 

        [HttpPost]
        public async Task<IActionResult> Login(Login model, string rolEsperado)
        {
            // 'rolEsperado' para saber a qué vista (Paciente, Medico, etc.) volver si falla.

            IActionResult resultado = View(rolEsperado, model);

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Username,
                    model.Password,
                    false,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    bool tienePermiso = await _userManager.IsInRoleAsync(user, rolEsperado);

                    if (tienePermiso)
                    {
                        // redirijo al portal correspondiente

                        if (rolEsperado == "Paciente")
                        {
                            resultado = RedirectToAction("Paciente", "Portal");
                        }
                        else if (rolEsperado == "Medico")
                        {
                            resultado = RedirectToAction("Medico", "Portal");
                        }
                        else if (rolEsperado == "Administrativo")
                        {
                            resultado = RedirectToAction("Administrativo", "Portal");
                        }
                        else
                        {
                            // si no es ninguno vuelve al inicio
                            resultado = RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        // Login OK, pero Rol Incorrecto
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "No tiene permisos para acceder a este portal.");

                    }
                }
                else
                {
                    // Contraseña o Usuario incorrecto
                    ModelState.AddModelError(string.Empty, "Datos de ingreso incorrectos.");

                }
            }

            return resultado;
        }


        //  LOGOUT

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
