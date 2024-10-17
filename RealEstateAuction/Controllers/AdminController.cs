using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAuction.DAL;
using RealEstateAuction.DataModel;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;

namespace RealEstateAuction.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserDAO userDAO;

        public AdminController(IMapper mapper)
        {
            userDAO = new UserDAO();
        }
        public IActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("manage-member")]
        public IActionResult ManageMember(int? page)
        {
            int PageNumber = page ?? 1;

            return View(userDAO.GetMember(PageNumber));
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("create-staff")]
        public IActionResult CreateStaff()
        {
            string fullName = Request.Form["fullName"];
            string email = Request.Form["email"];
            string pwd = Request.Form["pwd"];
            string phone = Request.Form["phone"];
            string date = Request.Form["dob"];

            User user = new User()
            {
                FullName = fullName,
                Email = email,
                Password = pwd,
                Phone = phone,
                Dob = DateTime.Parse(date),
                RoleId = (int)Roles.Staff,
                Address = "",
                Wallet = 0,
                Status = (int)Status.Active,
            };

            var exist = userDAO.GetUserByEmail(email);
            if (exist != null)
            {
                TempData["Message"] = "Email already exists!";
                ViewBag.User = user;
            }
            else
            {
                var result = userDAO.AddUser(user);
                if (result)
                {
                    TempData["Message"] = "Register successful!";
                }
                else
                {
                    TempData["Message"] = "Register fail!";
                }
            }

            return RedirectToAction("ManageStaff");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("update-user")]
        public IActionResult UpdateUser()
        {
            string curentUrl = HttpContext.Request.Headers["Referer"];

            var staff = userDAO.GetUserById(Int32.Parse(Request.Form["id"]));
            if (staff == null)
            {
                TempData["Message"] = "Thay đổi thất bại!";
            }
            else
            {
                staff.Status = Byte.Parse(Request.Form["status"]);
                var result = userDAO.UpdateUser(staff);
                Console.WriteLine(result);
                if (result)
                {
                    TempData["Message"] = "Thay đổi thành công!";
                }
                else
                {
                    TempData["Message"] = "Thay đổi thất bại!";
                }
            }

            return Redirect(curentUrl);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("manage-ticket")]
        public IActionResult ManageTicket(int? page)
        {
            int PageNumber = page ?? 1;
            ViewData["List"] = ticketDAO.listTicket(PageNumber);

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("manage-ticket/{id}")]
        public IActionResult TicketDetailAdmin(int id)
        {
            ViewData["Ticket"] = ticketDAO.ticketDetail(id);
            ViewData["Staffs"] = userDAO.GetStaff();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("assign-ticket")]
        public IActionResult AssignTicket([FromForm] AssignTicketDataModel assignTicketData)
        {
            if (ModelState.IsValid)
            {
                var ticket = ticketDAO.ticketDetail(int.Parse(assignTicketData.TicketId.ToString()));
                ticket.StaffId = int.Parse(assignTicketData.StaffId.ToString());
                ticketDAO.update(ticket);
                TempData["Message"] = "Bàn giao yêu cầu thành công";
            }
            else
            {
                TempData["Message"] = "Bàn giao yêu cầu thất bại";
            }
            return RedirectToAction("ManageTicket");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("list-auction-admin")]
        public IActionResult ListAuction(int? pageNumber)
        {
            ViewData["categories"] = categoryDAO.GetCategories();
            //check user login or not
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Message"] = "Vui lòng đăng nhập để quản lý đấu giá!";
                return RedirectToAction("Index", "Home");
            }

            if (pageNumber.HasValue)
            {
                pagination.PageNumber = pageNumber.Value;
            }

            //get auction by staff id
            List<Auction> auctions = auctionDAO.GetAuctionAdmin(pagination);

            int auctionCount = auctionDAO.CountAuctionAdmin();
            int pageSize = (int)Math.Ceiling((double)auctionCount / pagination.RecordPerPage);

            ViewBag.currentPage = pagination.PageNumber;
            ViewBag.pageSize = pageSize;

            return View(auctions);
        }
    }

}
