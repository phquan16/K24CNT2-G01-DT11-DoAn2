using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View("Index", new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View("Index", model);

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Sai tên đăng nhập hoặc mật khẩu.");
                return View("Index", model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToRoleHome(await _userManager.GetRolesAsync(user));
            }

            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "Tài khoản bị khóa tạm thời, vui lòng thử lại sau."
                : "Sai tên đăng nhập hoặc mật khẩu.");
            return View("Index", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToRoleHome(IList<string> roles)
        {
            if (roles.Contains(AppRoles.Admin))
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            if (roles.Contains(AppRoles.Cashier))
                return RedirectToAction("Index", "Home", new { area = "Cashier" });
            if (roles.Contains(AppRoles.Kitchen))
                return RedirectToAction("Index", "Queue", new { area = "Kitchen" });
            if (roles.Contains(AppRoles.Waiter))
                return RedirectToAction("Index", "Home", new { area = "Waiter" });

            return RedirectToAction("Index", "Home");
        }
    }
}
