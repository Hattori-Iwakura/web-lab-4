using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace web_lab_4.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserDetailsViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<Claim> Claims { get; set; } = new List<Claim>();
        public List<UserLoginInfo> Logins { get; set; } = new List<UserLoginInfo>();
        public object RecentOrders { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<UserRoleViewModel> UserRoles { get; set; } = new List<UserRoleViewModel>();
    }

    public class UserRoleViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}