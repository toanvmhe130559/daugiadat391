using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAuction.DAL;
using RealEstateAuction.DataModel;
using RealEstateAuction.Models;
using System.Security.Claims;


namespace RealEstateAuction.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserRepository userDAO;
        private IMapper _mapper;

        public AccountController(IMapper mapper)
        {
            userDAO = new UserRepository();
            _mapper = mapper;
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
    }
}
