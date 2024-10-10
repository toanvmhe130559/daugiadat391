using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAuction.DAL;
using RealEstateAuction.DataModel;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;
using RealEstateAuction.Services;
using System.Security.Claims;


namespace RealEstateAuction.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserDAO userDAO;
        private IMapper _mapper;

        public AccountController(IMapper mapper)
        {
            userDAO = new UserDAO();
            _mapper = mapper;
        }

        [HttpGet]
        [Route("profile")]
        public IActionResult Profile()
        {
            //Find User by Id
            int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            User user = userDAO.GetUserById(id);

            //map User to UserDatalModel
            UserDatalModel userData = _mapper.Map<User, UserDatalModel>(user);

            return View(userData);
        }

        [HttpPost]
        [Route("profile")]
        public IActionResult Profile(UserDatalModel userData)
        {
            if (!ModelState.IsValid)
            {
                return View(userData);
            }
            else
            {
                User user = _mapper.Map<UserDatalModel, User>(userData);
                //get old user
                User oldUser = userDAO.GetUserByEmail(user.Email);
                //update old user
                oldUser.FullName = user.FullName;
                oldUser.Phone = user.Phone;
                oldUser.Dob = user.Dob;
                oldUser.Address = user.Address;

                userDAO.UpdateUser(oldUser);
                TempData["Message"] = "Cập nhật thông tin cá nhân thành công!";
                return View(userData);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("change-password")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Route("change-password")]
        public IActionResult ChangePassword(ChangePasswordModel passwordData)
        {
            //if value of user enter is not valid
            if (!ModelState.IsValid)
            {
                return View();
            }
            else
            {
                //find User by Id
                int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                User user = userDAO.GetUserById(id);

                //verify old password of user
                if (!user.Password.Equals(passwordData.Password))
                {
                    ModelState.AddModelError("Password", "Mật khẩu cũ không đúng!");
                    return View();
                }
                else
                {
                    //update new password
                    userDAO.UpdatePassword(user.Email, passwordData.NewPassword);
                    TempData["Message"] = "Thay đổi mật khẩu thành công!";
                    return View();
                }
            }
        }
        [HttpPost("/create-ticket")]
        [Authorize(Roles = "Member")]
        public IActionResult CreateTicket([FromForm] TicketDataModel ticketData)
        {
            if (ModelState.IsValid)
            {
                Ticket ticket = new Ticket()
                {
                    UserId = Int32.Parse(User.FindFirstValue("Id")),
                    Title = ticketData.Title,
                    Description = ticketData.Description,
                    Status = (byte)TicketStatus.Opening,
                };
                if (ticketData.ImageFiles != null)
                {
                    List<TicketImage> images = new List<TicketImage>();
                    foreach (var file in ticketData.ImageFiles)
                    {
                        var pathImage = FileUpload.UploadImageProduct(file);
                        if (pathImage != null)
                        {
                            TicketImage image = new TicketImage();
                            image.Url = pathImage;
                            images.Add(image);
                        }
                    }
                    ticket.TicketImages = images;

                }

                ticketDAO.createTicket(ticket);
                TempData["Message"] = "Tạo yêu cầu thành công";
            }
            else
            {
                TempData["Message"] = "Tạo yêu cầu thất bại";
            }

            return RedirectToAction("ListTicketUser");
        }
    }
}
