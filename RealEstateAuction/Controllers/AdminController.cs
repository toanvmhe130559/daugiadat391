using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAuction.DAL;
using RealEstateAuction.DataModel;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;
using System.Security.Claims;

namespace RealEstateAuction.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserDAO userDAO;
        private readonly BankDAO bankDAO;
        private readonly TicketDAO ticketDAO;
        private readonly AuctionDAO auctionDAO;
        private readonly CategoryDAO categoryDAO;
        private Pagination pagination;

        public AdminController(IMapper mapper)
        {
            userDAO = new UserDAO();
            bankDAO = new BankDAO();
            ticketDAO = new TicketDAO();
            auctionDAO = new AuctionDAO();
            pagination = new Pagination();
            categoryDAO = new CategoryDAO();
        }
        public IActionResult Index()
        {
            return View();
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
        [HttpGet("manage-staff")]
        public IActionResult ManageStaff(int? page)
        {
            int PageNumber = page ?? 1;

            return View(userDAO.GetStaff(PageNumber));
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
        [HttpGet("manage-member")]
        public IActionResult ManageMember(int? page)
        {
            int PageNumber = page ?? 1;

            return View(userDAO.GetMember(PageNumber));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("details-user")]
        public IActionResult DetailsUser(int userId)
        {
            //check user login or not
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Message"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Index", "Home");
            }

            // Get user by Id
            User user = userDAO.GetUserById(userId);

            // Return view
            return View(user);
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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("details-auction")]
        public IActionResult DetailsAuction(int auctionId)
        {
            ViewData["categories"] = categoryDAO.GetCategories();
            //check user login or not
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Message"] = "Vui lòng đăng nhập để quản lý đấu giá!";
                return RedirectToAction("Index", "Home");
            }

            //get auction by Id
            Auction auction = auctionDAO.GetAuctionStaffById(auctionId);

            var winner = auctionDAO.GetWinner(auction);
            if (winner != null)
            {
                ViewData["Winner"] = winner;
            }
            return View(auction);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("approve-auction")]
        public IActionResult ListAuction(int auctionId, int status)
        {
            ViewData["categories"] = categoryDAO.GetCategories();
            //check user login or not
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Message"] = "Vui lòng đăng nhập để quản lý đấu giá!";
                return RedirectToAction("Index", "Home");
            }

            //get current user id
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            //get auction by Id
            Auction auction = auctionDAO.GetAuctionStaffById(auctionId);

            auction.Status = byte.Parse(status.ToString());

            //check if auction is rejected
            if (status == (int)AuctionStatus.Từ_chối)
            {
                auction.Reason = Request.Query["reason"];
            }
            else
            {
                auction.Categories.Add(categoryDAO.GetCategoryById(Int32.Parse(Request.Query["categoryId"])));
            }

            bool flag = auctionDAO.EditAuction(auction);
            if (flag)
            {
                TempData["Message"] = "Phê duyệt thành công!";
            }
            else
            {
                TempData["Message"] = "Phê duyệt thất bại";
            }

            return Redirect("list-auction-admin");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("confirm-auction")]
        public IActionResult ConfirmAuction(int auctionId, int status)
        {

            //get auction by Id
            Auction auction = auctionDAO.GetAuctionById(auctionId);

            if (auction.AuctionBiddings.Count > 0)
            {
                //get the price of winner
                var price = auctionDAO.GetMaxPrice(auctionId);

                var winnerId = auctionDAO.GetWinnerId(auction);

                //get the winner
                var winner = userDAO.GetUserById(winnerId);

                //check status
                if (status == 5)
                {
                    //update status of auction
                    auction.Status = byte.Parse(status.ToString());

                    //update auction
                    bool flag = auctionDAO.EditAuction(auction);

                    //check if update auction success
                    if (flag)
                    {
                        TempData["Message"] = "Cập nhật thành công!";
                    }
                    else
                    {
                        TempData["Message"] = "Cập nhật thất bại";
                    }
                }
                else
                {
                    //update status of auction
                    auction.Status = byte.Parse(status.ToString());

                    //update auction
                    bool flag = auctionDAO.EditAuction(auction);
                    if (flag)
                    {
                        TempData["Message"] = "Cập nhật thành công!";
                    }
                    else
                    {
                        TempData["Message"] = "Cập nhật thất bại";
                    }
                }
            }
            else
            {
                TempData["Message"] = "Phiên đấu giá không có người tham gia đặt giá";
            }

            return Redirect("list-auction-Admin");
        }
    }
}
