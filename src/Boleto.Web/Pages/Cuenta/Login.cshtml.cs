using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Boleto.Web.Pages.Cuenta;

[AllowAnonymous]
public class LoginModel(SignInManager<IdentityUser> signIn) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Falta el correo")]
        [EmailAddress(ErrorMessage = "Ese correo no tiene un formato válido")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Falta la contraseña")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool Recordarme { get; set; } = true;
    }

    public IActionResult OnGet()
        => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Panel/Index") : Page();

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var r = await signIn.PasswordSignInAsync(
            Input.Email, Input.Password, Input.Recordarme, lockoutOnFailure: true);

        if (r.Succeeded)
            return LocalRedirect(returnUrl ?? "/panel");

        // Mismo mensaje para usuario inexistente y clave mala: no se le dice
        // a un atacante cuál de los dos falló.
        Error = r.IsLockedOut
            ? "Demasiados intentos. Espera 15 minutos."
            : "Correo o contraseña incorrectos.";

        return Page();
    }
}
