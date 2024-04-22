using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartViewModel ShoppingCartViewModel { get; set; } 

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartViewModel = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId, "Product"),
                OrderHeader = new()
            };

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartViewModel);
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
            var dbShoppingCart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId, isTracked: true);
            if (dbShoppingCart.Count <= 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(sh => sh.ApplicationUserId == dbShoppingCart.ApplicationUserId).Count() - 1);
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
            var dbShoppingCart = _unitOfWork.ShoppingCart.Get(c => c.Id == cartId, isTracked: true);

            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(sh => sh.ApplicationUserId == dbShoppingCart.ApplicationUserId).Count() - 1);

            _unitOfWork.ShoppingCart.Remove(dbShoppingCart);
            
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartViewModel = new() 
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

			ShoppingCartViewModel.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			ShoppingCartViewModel.OrderHeader.Name = ShoppingCartViewModel.OrderHeader.ApplicationUser.Name;
			ShoppingCartViewModel.OrderHeader.StreetAddress = ShoppingCartViewModel.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartViewModel.OrderHeader.City = ShoppingCartViewModel.OrderHeader.ApplicationUser.City;
			ShoppingCartViewModel.OrderHeader.State = ShoppingCartViewModel.OrderHeader.ApplicationUser.State;
			ShoppingCartViewModel.OrderHeader.PostalCode = ShoppingCartViewModel.OrderHeader.ApplicationUser.PostalCode;
			ShoppingCartViewModel.OrderHeader.PhoneNumber = ShoppingCartViewModel.OrderHeader.ApplicationUser.PhoneNumber;

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }


            return View(ShoppingCartViewModel);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartViewModel.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId, includeProperties: "Product");
            ShoppingCartViewModel.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCartViewModel.OrderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartViewModel.OrderHeader);
            _unitOfWork.Save();

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {

                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    Count = cart.Count,
                    Price = cart.Price,
                    OrderHeaderId = ShoppingCartViewModel.OrderHeader.Id
                };

                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
				string domain = Request.Scheme + "://" + Request.Host.Value + "/";
				var options = new Stripe.Checkout.SessionCreateOptions
                {
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartViewModel.OrderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};
                
                foreach (var item in ShoppingCartViewModel.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

				var service = new Stripe.Checkout.SessionService();
				Session session = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartViewModel.OrderHeader.Id,session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
			}

            return RedirectToAction(nameof(OrderConfirmation), new {id = ShoppingCartViewModel.OrderHeader.Id});
        }

        public IActionResult OrderConfirmation(int id)
        {

            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }

                HttpContext.Session.Clear();

            }

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);
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
