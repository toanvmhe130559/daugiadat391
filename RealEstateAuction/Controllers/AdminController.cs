using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Plugins;
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
    }
}
