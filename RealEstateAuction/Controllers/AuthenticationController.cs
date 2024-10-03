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
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword()
        {
            var user = userDAO.GetUserByEmail(Request.Form["email"]);
            if (user == null)
            {
                TempData["Message"] = "Email does not exist!";
                return Redirect("home");
            }
            else
            {
                string email = Request.Form["email"];

                // Generate a random OTP (6-digit number)
                Random random = new Random();
                int otpNumber = random.Next(0, 1000000);
                string otp = otpNumber.ToString("D6");

                // Save the OTP into session
                HttpContext.Session.SetString("Otp", otp);

                // Save user email into session
                HttpContext.Session.SetString("email", email);

                //send email
                _emailSender.SendEmailAsync(email, "Reset your password!",
                    "\nEnter OTP to change your password!" +
                    "\nYour OTP: " + otp);

                //redirect to enter otp page
                return Redirect("enter-otp");
            }
        }
        [HttpGet("reset-password")]
        public IActionResult ResetPassword()
        {
            return View();
        }
        [HttpPost("enter-otp")]
        public IActionResult EnterOtp(IFormCollection formCollection)
        {
            string otp = formCollection["otp"];
            //check otp is correct or not
            if (HttpContext.Session.GetString("Otp").Equals(otp))
            {
                HttpContext.Session.Remove("Otp");
                return Redirect("reset-password");
            }
            else
            {
                TempData["Message"] = "Wrong OTP!";
                return View();
            }
        }

    }
}
