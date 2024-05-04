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

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, 
            RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
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

            var user = _userManager.FindByIdAsync(id).GetAwaiter().GetResult();

            if (user == null)
            {
                return NotFound();
            }

            var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();

            ManageUserViewModel manageUserViewModel = new ManageUserViewModel
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == id, includeProperties:"Company"),
                RoleList = _roleManager.Roles.Select(r => r.Name).Select(r => new SelectListItem
                {
                    Text = r,
                    Value = r
                }),

                CompanyList = _unitOfWork.Company.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            manageUserViewModel.ApplicationUser.Role = roles.FirstOrDefault();

            return View(manageUserViewModel);
        }

        [HttpPost]
        public IActionResult Permission(ManageUserViewModel model)
        {
            var user = _unitOfWork.ApplicationUser.Get(u => u.Id == model.ApplicationUser.Id,isTracked: true);

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

            _unitOfWork.Save();

            return RedirectToAction("Index");

        }


        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> users = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();

            foreach (var user in users)
            {
                var role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

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
            var user = _unitOfWork.ApplicationUser.Get(u => u.Id == id, isTracked: true); 

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

            _unitOfWork.Save();

            return Json(new { success = true, message = "Operation Successfull" });
        }
        #endregion
    }
}
