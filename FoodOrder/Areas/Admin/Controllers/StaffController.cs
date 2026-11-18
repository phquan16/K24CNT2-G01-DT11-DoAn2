using System.Security.Claims;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class StaffController : Controller
{
    private static readonly string[] AllRoles =
    {
        AppRoles.Admin, AppRoles.Waiter, AppRoles.Kitchen, AppRoles.Cashier
    };

    private readonly UserManager<ApplicationUser> _users;

    public StaffController(UserManager<ApplicationUser> users) => _users = users;

    public async Task<IActionResult> Index()
    {
        var users = await _users.Users.OrderBy(u => u.UserName).ToListAsync();
        var rows = new List<StaffRowViewModel>();

        foreach (var user in users)
        {
            var roles = await _users.GetRolesAsync(user);
            rows.Add(new StaffRowViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                Role = roles.FirstOrDefault() ?? "(chưa gán)"
            });
        }

        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        LoadRoles();
        return View("Form", new StaffFormViewModel { Role = AppRoles.Waiter, IsActive = true });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _users.GetRolesAsync(user);
        LoadRoles();
        return View("Form", new StaffFormViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? AppRoles.Waiter,
            IsActive = user.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(StaffFormViewModel vm)
    {
        if (!AllRoles.Contains(vm.Role))
            ModelState.AddModelError(nameof(vm.Role), "Vai trò không hợp lệ.");

        var isCreate = string.IsNullOrEmpty(vm.Id);
        if (isCreate && string.IsNullOrWhiteSpace(vm.Password))
            ModelState.AddModelError(nameof(vm.Password), "Vui lòng nhập mật khẩu.");

        if (!ModelState.IsValid)
        {
            LoadRoles();
            return View("Form", vm);
        }

        return isCreate ? await CreateAsync(vm) : await UpdateAsync(vm);
    }

    private async Task<IActionResult> CreateAsync(StaffFormViewModel vm)
    {
        var user = new ApplicationUser
        {
            UserName = vm.UserName.Trim(),
            Email = vm.Email.Trim(),
            FullName = vm.FullName.Trim(),
            IsActive = vm.IsActive
        };

        var created = await _users.CreateAsync(user, vm.Password!);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors) ModelState.AddModelError(string.Empty, Describe(error));
            LoadRoles();
            return View("Form", vm);
        }

        var roleResult = await _users.AddToRoleAsync(user, vm.Role);
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, Describe(error));
            LoadRoles();
            return View("Form", vm);
        }

        TempData["Success"] = $"Đã tạo tài khoản {user.UserName}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> UpdateAsync(StaffFormViewModel vm)
    {
        var user = await _users.FindByIdAsync(vm.Id!);
        if (user == null) return NotFound();

        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSelf = user.Id == currentId;

        if (isSelf && !vm.IsActive)
            ModelState.AddModelError(nameof(vm.IsActive), "Không thể tự khóa tài khoản đang đăng nhập.");
        if (isSelf && vm.Role != AppRoles.Admin)
            ModelState.AddModelError(nameof(vm.Role), "Không thể tự bỏ quyền Admin của chính mình.");

        if (!ModelState.IsValid)
        {
            vm.UserName = user.UserName ?? vm.UserName;
            LoadRoles();
            return View("Form", vm);
        }

        var emailChanged = !string.Equals(user.Email, vm.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var emailResult = await _users.SetEmailAsync(user, vm.Email.Trim());
            if (!emailResult.Succeeded)
            {
                foreach (var error in emailResult.Errors) ModelState.AddModelError(nameof(vm.Email), Describe(error));
                vm.UserName = user.UserName ?? vm.UserName;
                LoadRoles();
                return View("Form", vm);
            }
        }

        var roles = await _users.GetRolesAsync(user);
        var managedRoles = roles.Where(r => AllRoles.Contains(r)).ToList();
        if (!managedRoles.Contains(vm.Role))
        {
            if (managedRoles.Count > 0)
            {
                var remove = await _users.RemoveFromRolesAsync(user, managedRoles);
                if (!remove.Succeeded)
                {
                    AddErrors(remove);
                    vm.UserName = user.UserName ?? vm.UserName;
                    LoadRoles();
                    return View("Form", vm);
                }
            }

            var add = await _users.AddToRoleAsync(user, vm.Role);
            if (!add.Succeeded)
            {
                AddErrors(add);
                vm.UserName = user.UserName ?? vm.UserName;
                LoadRoles();
                return View("Form", vm);
            }
        }

        user.FullName = vm.FullName.Trim();
        user.IsActive = vm.IsActive;
        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            AddErrors(updated);
            vm.UserName = user.UserName ?? vm.UserName;
            LoadRoles();
            return View("Form", vm);
        }

        await _users.UpdateSecurityStampAsync(user);
        TempData["Success"] = $"Đã cập nhật tài khoản {user.UserName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();
        ViewData["UserName"] = user.UserName;
        return View(new ResetPasswordViewModel { Id = user.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _users.FindByIdAsync(vm.Id);
        if (user == null) return NotFound();

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, Describe(error));
            ViewData["UserName"] = user.UserName;
            return View(vm);
        }

        TempData["Success"] = $"Đã đặt lại mật khẩu cho {user.UserName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] = "Không thể tự khóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        var result = await _users.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _users.UpdateSecurityStampAsync(user);
            TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(Describe));
        }

        return RedirectToAction(nameof(Index));
    }

    private void LoadRoles() => ViewBag.Roles = AllRoles;

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, Describe(error));
    }

    private static string Describe(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" => "Tên đăng nhập đã tồn tại.",
        "DuplicateEmail" => "Email đã được sử dụng.",
        "PasswordTooShort" => "Mật khẩu quá ngắn.",
        "PasswordRequiresDigit" => "Mật khẩu phải có chữ số.",
        "PasswordRequiresUpper" => "Mật khẩu phải có chữ in hoa.",
        "PasswordRequiresLower" => "Mật khẩu phải có chữ thường.",
        "InvalidEmail" => "Email không hợp lệ.",
        _ => error.Description
    };
}
