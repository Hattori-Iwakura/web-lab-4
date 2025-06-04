using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_lab_4.Data;
using web_lab_4.Models;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // Display all users
        public async Task<IActionResult> Index(string searchTerm = "", string roleFilter = "", string statusFilter = "")
        {
            var users = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "active")
                {
                    users = users.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.Now);
                }
                else if (statusFilter == "locked")
                {
                    users = users.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.Now);
                }
                else if (statusFilter == "unconfirmed")
                {
                    users = users.Where(u => !u.EmailConfirmed);
                }
            }

            var userList = await users.OrderBy(u => u.UserName).ToListAsync();

            // Get user roles and statistics
            var userViewModels = new List<UserViewModel>();
            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var orderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);
                var totalSpent = await _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    CreatedDate = user.LockoutEnd ?? DateTimeOffset.Now, // Placeholder
                    Roles = roles.ToList(),
                    OrderCount = orderCount,
                    TotalSpent = totalSpent,
                    IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.Now
                });
            }

            // Set ViewBag data for filters
            ViewBag.SearchTerm = searchTerm;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.Roles = await _roleManager.Roles.ToListAsync();

            // Statistics
            ViewBag.TotalUsers = userViewModels.Count;
            ViewBag.ActiveUsers = userViewModels.Count(u => u.IsActive);
            ViewBag.LockedUsers = userViewModels.Count(u => !u.IsActive);
            ViewBag.UnconfirmedUsers = userViewModels.Count(u => !u.EmailConfirmed);

            return View(userViewModels);
        }

        // User details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var logins = await _userManager.GetLoginsAsync(user);

            // Get user orders
            var orders = await _context.Orders
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new {
                    o.Id,
                    o.OrderDate,
                    o.TotalPrice,
                    o.Status
                })
                .ToListAsync();

            var userDetails = new UserDetailsViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles.ToList(),
                Claims = claims.ToList(),
                Logins = logins.ToList(),
                RecentOrders = orders,
                TotalOrders = await _context.Orders.CountAsync(o => o.UserId == id),
                TotalSpent = await _context.Orders.Where(o => o.UserId == id).SumAsync(o => (decimal?)o.TotalPrice) ?? 0
            };

            return View(userDetails);
        }

        // Manage user roles
        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                UserRoles = allRoles.Select(role => new UserRoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = userRoles.Contains(role.Name)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var roleModel in model.UserRoles)
            {
                if (roleModel.IsSelected && !userRoles.Contains(roleModel.RoleName))
                {
                    await _userManager.AddToRoleAsync(user, roleModel.RoleName);
                }
                else if (!roleModel.IsSelected && userRoles.Contains(roleModel.RoleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, roleModel.RoleName);
                }
            }

            TempData["SuccessMessage"] = $"Roles updated successfully for {user.UserName}";
            return RedirectToAction(nameof(Details), new { id = model.UserId });
        }

        // Lock/Unlock user
        [HttpPost]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now)
            {
                // Unlock user
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"User {user.UserName} has been unlocked.";
            }
            else
            {
                // Lock user for 1 year
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(1));
                TempData["SuccessMessage"] = $"User {user.UserName} has been locked.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Confirm email
        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _userManager.ConfirmEmailAsync(user, token);

            TempData["SuccessMessage"] = $"Email confirmed for {user.UserName}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Reset password
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = GenerateRandomPassword();
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Password reset for {user.UserName}. New password: {newPassword}";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reset password.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Delete user
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user has orders
            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
            if (hasOrders)
            {
                TempData["ErrorMessage"] = "Cannot delete user with existing orders. Lock the user instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User {user.UserName} has been deleted.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to delete user.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Get user statistics for dashboard
        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.Now);
            var lockedUsers = totalUsers - activeUsers;
            var unconfirmedUsers = await _userManager.Users.CountAsync(u => !u.EmailConfirmed);

            var recentUsers = await _userManager.Users
                .OrderByDescending(u => u.Id) // Assuming newer users have higher IDs
                .Take(5)
                .Select(u => new {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.EmailConfirmed,
                    IsActive = !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.Now
                })
                .ToListAsync();

            return Json(new {
                totalUsers,
                activeUsers,
                lockedUsers,
                unconfirmedUsers,
                recentUsers
            });
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}