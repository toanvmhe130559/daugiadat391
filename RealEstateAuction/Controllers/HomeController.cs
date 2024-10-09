using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RealEstateAuction.DAL;
using RealEstateAuction.DataModel;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;
using System.Data;
using System.Diagnostics;

namespace RealEstateAuction.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AuctionDAO auctionDAO;
        private readonly Pagination pagination;
        private readonly CategoryDAO categoryDAO;

        public HomeController(ILogger<HomeController> logger)
        {
            pagination = new Pagination();
            auctionDAO = new AuctionDAO();
            categoryDAO = new CategoryDAO();
            _logger = logger;
        }
        [Route("")]
        [Route("home")]
        public IActionResult Index()
        {
            //get 6 auction recently to display on hompage
            List<Auction> auctionRecent = auctionDAO.GetAuctionRecently(6);

            return View(auctionRecent);
        }


        [HttpGet]
        [Route("denied")]
        public IActionResult AccessDenied()
        {
            TempData["Message"] = "Bạn không có quyền truy cập trang này";
            return Redirect("/home");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}