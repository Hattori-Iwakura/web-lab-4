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

        private readonly ILogger<UserController> _logger;
        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
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
                switch (statusFilter)
                {
                    case "active":
                        users = users.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.Now);
                        break;
                    case "locked":
                        users = users.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.Now);
                        break;
                    case "unconfirmed":
                        users = users.Where(u => !u.EmailConfirmed);
                        break;
                }
            }

            var userList = await users.OrderBy(u => u.UserName).ToListAsync();

            // Get user roles and statistics
            var userViewModels = new List<UserViewModel>();
            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                // Apply role filter after getting roles
                if (!string.IsNullOrEmpty(roleFilter) && !roles.Contains(roleFilter))
                {
                    continue;
                }

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
                    CreatedDate = DateTimeOffset.Now, // TODO: Add actual creation date tracking
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
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var logins = await _userManager.GetLoginsAsync(user);

            // Get user orders with error handling
            var orders = new List<dynamic>();
            try
            {
                orders = await _context.Orders
                    .Where(o => o.UserId == id)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new {
                        o.Id,
                        o.OrderDate,
                        o.TotalPrice,
                        o.Status
                    })
                    .ToListAsync<dynamic>();
            }
            catch (Exception)
            {
                // Handle case where Orders table might not exist or have different structure
                orders = new List<dynamic>();
            }

            var totalOrders = 0;
            var totalSpent = 0m;
            
            try
            {
                totalOrders = await _context.Orders.CountAsync(o => o.UserId == id);
                totalSpent = await _context.Orders
                    .Where(o => o.UserId == id)
                    .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;
            }
            catch (Exception)
            {
                // Handle case where Orders table might not exist
                totalOrders = 0;
                totalSpent = 0m;
            }

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
                TotalOrders = totalOrders,
                TotalSpent = totalSpent
            };

            return View(userDetails);
        }

        // Manage user roles
        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles for user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading user roles.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserId))
            {
                TempData["ErrorMessage"] = "Invalid data provided.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Prevent admin from removing their own admin role
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == user.Id)
                {
                    var adminRole = model.UserRoles?.FirstOrDefault(r => r.RoleName == "Admin");
                    if (adminRole != null && !adminRole.IsSelected)
                    {
                        TempData["ErrorMessage"] = "You cannot remove your own admin role.";
                        return RedirectToAction(nameof(ManageRoles), new { id = model.UserId });
                    }
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var errors = new List<string>();

                if (model.UserRoles != null)
                {
                    foreach (var roleModel in model.UserRoles)
                    {
                        if (roleModel.IsSelected && !userRoles.Contains(roleModel.RoleName))
                        {
                            var result = await _userManager.AddToRoleAsync(user, roleModel.RoleName);
                            if (!result.Succeeded)
                            {
                                errors.AddRange(result.Errors.Select(e => e.Description));
                            }
                        }
                        else if (!roleModel.IsSelected && userRoles.Contains(roleModel.RoleName))
                        {
                            var result = await _userManager.RemoveFromRoleAsync(user, roleModel.RoleName);
                            if (!result.Succeeded)
                            {
                                errors.AddRange(result.Errors.Select(e => e.Description));
                            }
                        }
                    }
                }

                if (errors.Any())
                {
                    TempData["ErrorMessage"] = "Some role updates failed: " + string.Join(", ", errors);
                }
                else
                {
                    TempData["SuccessMessage"] = $"Roles updated successfully for {user.UserName}";
                }

                return RedirectToAction(nameof(Details), new { id = model.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roles for user {UserId}", model.UserId);
                TempData["ErrorMessage"] = "An error occurred while updating user roles.";
                return RedirectToAction(nameof(Details), new { id = model.UserId });
            }
        }

        // Lock/Unlock user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Prevent admin from locking themselves
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == user.Id)
            {
                return Json(new { success = false, message = "You cannot lock your own account." });
            }

            try
            {
                IdentityResult result;
                
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now)
                {
                    // Unlock user
                    result = await _userManager.SetLockoutEndDateAsync(user, null);
                    if (result.Succeeded)
                    {
                        // Reset failed access count when unlocking
                        await _userManager.ResetAccessFailedCountAsync(user);
                        return Json(new { success = true, message = $"User {user.UserName} has been unlocked." });
                    }
                }
                else
                {
                    // Lock user for 1 year
                    var lockoutEnd = DateTimeOffset.Now.AddYears(1);
                    result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
                    if (result.Succeeded)
                    {
                        return Json(new { success = true, message = $"User {user.UserName} has been locked until {lockoutEnd:yyyy-MM-dd}." });
                    }
                }
                
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = "Operation failed: " + errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while toggling lock for user {UserName}", user.UserName);
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // Confirm email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail(string id)
        {
            _logger.LogInformation("ConfirmEmail called for user ID: {UserId}", id);
            
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (user.EmailConfirmed)
            {
                return Json(new { success = false, message = "Email is already confirmed." });
            }

            try
            {
                // Manually confirm email without token
                user.EmailConfirmed = true;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmed successfully for user {UserName}", user.UserName);
                    return Json(new { success = true, message = $"Email confirmed for {user.UserName}" });
                }
                
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to confirm email for user {UserName}: {Errors}", user.UserName, errors);
                return Json(new { success = false, message = "Failed to confirm email: " + errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while confirming email for user {UserName}", user.UserName);
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // Reset password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            try
            {
                // Generate new random password
                var newPassword = GenerateRandomPassword();
                
                // Remove existing password and set new one
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    return Json(new { 
                        success = false, 
                        message = "Failed to remove old password: " + string.Join(", ", removePasswordResult.Errors.Select(e => e.Description))
                    });
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
                if (addPasswordResult.Succeeded)
                {
                    return Json(new { 
                        success = true, 
                        message = $"Password reset for {user.UserName}",
                        newPassword = newPassword 
                    });
                }
                return Json(new { 
                    success = false, 
                    message = "Failed to set new password: " + string.Join(", ", addPasswordResult.Errors.Select(e => e.Description))
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // Delete user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Prevent admin from deleting themselves
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == user.Id)
            {
                return Json(new { success = false, message = "You cannot delete your own account." });
            }

            // Check if user has orders
            try
            {
                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (hasOrders)
                {
                    return Json(new { 
                        success = false, 
                        message = "Cannot delete user with existing orders. Lock the user instead." 
                    });
                }
            }
            catch (Exception)
            {
                // If Orders table doesn't exist, proceed with deletion
            }

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { 
                        success = true, 
                        message = $"User {user.UserName} has been deleted.",
                        redirect = true
                    });
                }
                return Json(new { 
                    success = false, 
                    message = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while deleting user." });
            }
        }

        // Get user statistics for dashboard
        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var totalUsers = await _userManager.Users.CountAsync();
                var activeUsers = await _userManager.Users.CountAsync(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.Now);
                var lockedUsers = totalUsers - activeUsers;
                var unconfirmedUsers = await _userManager.Users.CountAsync(u => !u.EmailConfirmed);

                var recentUsers = await _userManager.Users
                    .OrderByDescending(u => u.Id)
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
                    success = true,
                    totalUsers,
                    activeUsers,
                    lockedUsers,
                    unconfirmedUsers,
                    recentUsers
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Failed to retrieve user statistics." });
            }
        }

        private string GenerateRandomPassword()
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*";
            
            var random = new Random();
            var password = new List<char>();

            // Ensure password meets complexity requirements
            password.Add(upperCase[random.Next(upperCase.Length)]);
            password.Add(lowerCase[random.Next(lowerCase.Length)]);
            password.Add(digits[random.Next(digits.Length)]);
            password.Add(specialChars[random.Next(specialChars.Length)]);

            // Fill remaining positions
            const string allChars = upperCase + lowerCase + digits + specialChars;
            for (int i = 4; i < 12; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            for (int i = password.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password.ToArray());
        }
    }
}