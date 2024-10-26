using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RealEstateAuction.DAL;
using RealEstateAuction.DataModel;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;
using RealEstateAuction.Services;
using System.Security.Claims;

namespace RealEstateAuction.Controllers
{
    public class StaffController : Controller
    {
        private readonly PaymentDAO paymentDAO;
        private readonly TicketDAO ticketDAO;
        private readonly UserDAO userDAO;
        private Pagination pagination;
        private readonly AuctionDAO auctionDAO;
        private readonly CategoryDAO categoryDAO;
        private IMapper _mapper;
        private readonly AuctionBiddingDAO auctionBiddingDAO;

        public StaffController(IMapper mapper)
        {
            auctionDAO = new AuctionDAO();
            pagination = new Pagination();
            userDAO = new UserDAO();
            ticketDAO = new TicketDAO();
            paymentDAO = new PaymentDAO();
            categoryDAO = new CategoryDAO();
            _mapper = mapper;
            auctionBiddingDAO = new AuctionBiddingDAO();
        }
        [HttpGet("list-participant")]
        [Authorize(Roles = "Staff")]
        public IActionResult ListParticipant(int? auctionId, int? pageNumber)
        {
            ViewBag.isFinished = false;

            //Check if auctionId is null
            if (!auctionId.HasValue)
            {
                TempData["Message"] = "Phiên đấu giá không tồn tại";
                return RedirectToAction("ManageAuction");
            }

            //Get auction by id
            var auction = auctionDAO.GetAuctionById(auctionId.Value);

            if (auction == null)
            {
                TempData["Message"] = "Phiên đấu giá không tồn tại";
                return RedirectToAction("ManageAuction");
            }

            //Get current userId
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            //Check if auction belong to this user
            if (auction.UserId != userId)
            {
                TempData["Message"] = "Bạn không thể xem danh sách người tham gia của phiên đấu giá này!";
                return RedirectToAction("ManageAuction");
            }

            //Check auction is finished
            if (auction.Status == (int)AuctionStatus.Kết_thúc)
            {
                ViewBag.isFinished = true;
            }


            if (pageNumber.HasValue)
            {
                pagination.PageNumber = pageNumber.Value;
            }

            //Get list of participant
            var list = auctionBiddingDAO.GetParticipantByAuctionId(auctionId.Value, pagination);

            //Count number of participant
            int participantCount = auctionBiddingDAO.CountParticipant(auctionId.Value);

            //Get number of page
            int pageSize = (int)Math.Ceiling((double)participantCount / pagination.RecordPerPage);

            ViewBag.currentPage = pagination.PageNumber;
            ViewBag.pageSize = pageSize;

            return View(list);
        }
    }
}