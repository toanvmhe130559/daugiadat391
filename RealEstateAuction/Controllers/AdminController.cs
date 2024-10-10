using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAuction.DAL;
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
    }
}
