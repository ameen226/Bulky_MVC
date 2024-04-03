using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Company> companies = _unitOfWork.Company.GetAll().ToList();
            return View(companies);
        }

        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new Company());
            }

            Company company = _unitOfWork.Company.Get(c => c.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);

        }

        [HttpPost]
        public IActionResult Upsert(Company obj)
        {

            if (ModelState.IsValid)
            {
                if (obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    TempData["success"] = "Company created successfully";
                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                    TempData["success"] = "Company updated successfully";
                }

                _unitOfWork.Save();
                return RedirectToAction("Index");
            }

            return View(obj);
        }

        #region API Call

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companies = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = companies });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return Json("NotFound");
            }

            Company company = _unitOfWork.Company.Get(c => c.Id == id);

            if (company == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Company.Remove(company);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successfull" });

        }

        

        #endregion
    }
}
