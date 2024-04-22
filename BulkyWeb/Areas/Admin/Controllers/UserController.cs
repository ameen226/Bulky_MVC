using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {

        private readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
        }


        public IActionResult Index()
        {
            return View();
        }

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

    }
}
