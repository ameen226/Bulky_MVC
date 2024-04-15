using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var userId = claim.Value;
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(sh => sh.ApplicationUserId == userId).Count());
            }




            IEnumerable<Product> products = _unitOfWork.Product.GetAll(includeProperties:"Category");
            return View(products);
        }

        public IActionResult Details(int id)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unitOfWork.Product.Get(p => p.Id == id, "Category"),
                Count = 1,
                ProductId = id
            };
            return View(shoppingCart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCart cart = _unitOfWork.ShoppingCart.Get(c => c.ProductId == shoppingCart.ProductId && c.ApplicationUserId == userId);

            if (cart == null)
            {
                shoppingCart.Id = 0;
                shoppingCart.ApplicationUserId = userId;
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(s => s.ApplicationUserId == userId).Count());

            }
            else
            {
                cart.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cart);
                _unitOfWork.Save();
            }

            TempData["success"] = "Cart updated successfully";

            

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
