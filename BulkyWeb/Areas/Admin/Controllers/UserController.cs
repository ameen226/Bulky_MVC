using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {

        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Permission(string? id)
        {
            
            var roleId = _db.UserRoles.FirstOrDefault(ur => ur.UserId == id)?.RoleId;
            var role = _db.Roles.FirstOrDefault(r => r.Id == roleId)?.Name;

            ManageUserViewModel manageUserViewModel = new ManageUserViewModel
            {
                ApplicationUser = _db.ApplicationUsers.Include("Company").FirstOrDefault(u => u.Id == id),
                RoleList = _db.Roles.Select(r => r.Name).Select(r => new SelectListItem
                {
                    Text = r,
                    Value = r
                }),

                CompanyList = _db.Companies.Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            manageUserViewModel.ApplicationUser.Role = role;

            return View(manageUserViewModel);
        }

        [HttpPost]
        public IActionResult Permission(ManageUserViewModel model)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == model.ApplicationUser.Id);

            var currentRole = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
            _userManager.RemoveFromRolesAsync(user, currentRole).GetAwaiter().GetResult();

            var roleExists = _roleManager.RoleExistsAsync(model.ApplicationUser.Role).GetAwaiter().GetResult();

            if (!roleExists)
            {
                return NotFound("Role Does Not Exist");
            }

            _userManager.AddToRoleAsync(user, model.ApplicationUser.Role).GetAwaiter().GetResult();
            user.CompanyId = null;

            if (model.ApplicationUser.Role == "Company")
            {
                user.CompanyId = model.ApplicationUser.CompanyId;
            }

            _db.SaveChanges();

            return RedirectToAction("Index");

        }


        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> users = _db.ApplicationUsers.Include(u => u.Company).ToList();

            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (var user in users)
            {

                var roleId = userRoles.FirstOrDefault(ur => ur.UserId == user.Id).RoleId;
                var role = roles.FirstOrDefault(r => r.Id == roleId).Name;

                user.Role = role;

                if (user.Company == null)
                {
                    user.Company = new Company { Name = "" };
                }
            }
            return Json(new { data = users });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            {
                user.LockoutEnd = DateTime.Now;
            }
            else
            {
                user.LockoutEnd = DateTime.Now.AddYears(1000);
            }

            _db.SaveChanges();

            return Json(new { success = true, message = "Operation Successfull" });
        }
        #endregion
    }
}
