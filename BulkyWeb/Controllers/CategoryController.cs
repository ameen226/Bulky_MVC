using BulkyWeb.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace BulkyWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            List<Category> categories = _db.Categories.ToList();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (ModelState.IsValid)
            {
                _db.Categories.Add(obj);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = _db.Categories.Find(id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            return View(categoryFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Category updatedCategory)
        {
            
            if (ModelState.IsValid)
            {
                _db.Categories.Update(updatedCategory);
                _db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(updatedCategory);
            
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? foundCategory = _db.Categories.Find(id);

            if (foundCategory == null)
            {
                return NotFound();
            }

            return View(foundCategory);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? foundCategory = _db.Categories.Find(id);

            if (foundCategory == null)
            {
                return NotFound();
            }

            _db.Categories.Remove(foundCategory);
            _db.SaveChanges();

            return RedirectToAction("Index");
        }


    }
}
