using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class OrderController: Controller
	{

		private readonly IUnitOfWork _unitOfWork;

		public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			return View();
		}


		#region API Call

		[HttpGet]
		public IActionResult GetAll(string? status)
		{
			IEnumerable<OrderHeader> orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");

			switch(status)
			{
				case "pending":
					orderHeaders = orderHeaders.Where(o => o.PaymentStatus == SD.PaymentStatusPending);
					break;
				case "inprocess":
					orderHeaders = orderHeaders.Where(o => o.OrderStatus == SD.StatusInProcess);
					break;
				case "completed":
					orderHeaders = orderHeaders.Where(o => o.OrderStatus == SD.StatusShipped);
					break;
				case "approved":
					orderHeaders = orderHeaders.Where(o => o.OrderStatus == SD.StatusApproved);
					break;
				default:
					break;
			}

			return Json(new { data = orderHeaders });
		}

		#endregion

	}
}
