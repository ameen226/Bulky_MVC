using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private ShoppingCartViewModel _shoppingCartViewModel; 

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            _shoppingCartViewModel = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId, "Product")
            };

            foreach (var cart in _shoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                _shoppingCartViewModel.OrderTotal += (cart.Price * cart.Count);
            }

            return View(_shoppingCartViewModel);
        }

        public IActionResult Plus(int cartId)
        {
            var dbShoppingCart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
            dbShoppingCart.Count += 1;

            _unitOfWork.ShoppingCart.Update(dbShoppingCart);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var dbShoppingCart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
            if (dbShoppingCart.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(dbShoppingCart);
            }
            else
            {
                dbShoppingCart.Count -= 1;
                _unitOfWork.ShoppingCart.Update(dbShoppingCart);
            }
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Remove(int cartId)
        {
            var dbShoppingCart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(dbShoppingCart);

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary()
        {
            return View();
        }



        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
        }

    }
}
