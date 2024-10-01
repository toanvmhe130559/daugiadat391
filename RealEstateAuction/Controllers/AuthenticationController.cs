using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using RealEstateAuction.DAL;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;
using RealEstateAuction.Services;
using System.Security.Claims;

namespace RealEstateAuction.Controllers
{
    public class AuthenticationController : Controller
    {
        UserRepository userDAO = new UserRepository();
        private readonly IEmailSender _emailSender;

        public AuthenticationController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login()
        {
            string curentUrl = HttpContext.Request.Headers["Referer"];

            string email = Request.Form["email"];
            string password = Request.Form["pwd"];

            var user = userDAO.GetUserByEmailAndPassword(email, password);

            var roles = new Dictionary<Roles, string>
            {
                { Roles.Admin, "Admin" },
                { Roles.Staff, "Staff" },
                { Roles.Member, "Member" }
            };

            Console.WriteLine(user);
            if (user != null)
            {
                List<Claim> claims = new List<Claim>() {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, roles[(Roles) user.RoleId]),
                    new Claim("Id", user.Id.ToString()),
                    new Claim("FullName", user.FullName),
                    new Claim("Wallet", user.Wallet.ToString()),
                    //new Claim("Email", user.Email),
                    //new Claim("Phone", user.Phone),
                    //new Claim("Dob", user.Dob.ToString()),
                    //new Claim("Address", user.Address),
                };


                ClaimsIdentity claimsIdentity =
                    new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);


                AuthenticationProperties properties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    /*ExpiresUtc = System.DateTimeOffset.UtcNow.AddMinutes(20),*/

                    //Keep login
                    /*IsPersistent = true,*/
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                          new ClaimsPrincipal(claimsIdentity), properties);

                TempData["Message"] = "Login successful!";
                switch (user.RoleId)
                {
                    case (int)Roles.Admin:
                        return Redirect("admin");
                    case (int)Roles.Staff:
                        return Redirect("staff");
                    default:
                        return Redirect(curentUrl);
                }              
            }
            else
            {
                TempData["Message"] = "Login fail!";
                return Redirect(curentUrl);
            }
        }

        
    }
}
