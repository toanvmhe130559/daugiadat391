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

        [Authorize(Roles = "Staff")]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Staff")]
        [HttpGet("/list-payment")]
        public IActionResult ListPayment(int? page)
        {
            int PageNumber = (page ?? 1);
            var list = paymentDAO.list(PageNumber);
            return View(list);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("/list-payment/update")]
        public IActionResult UpdatePayment()
        {
            try
            {
                var payment = paymentDAO.getPayment(Int32.Parse(Request.Form["id"].ToString()));

                if (payment != null && payment.Status == (int) PaymentStatus.Pending)
                {
                    payment.Status = Byte.Parse(Request.Form["status"].ToString());
                    paymentDAO.topUp(payment, (int)payment.UserId, Int32.Parse(User.FindFirstValue("Id")));
                               
                    TempData["Message"] = "Cập nhật thành công";
                }
                else
                {
                    TempData["Message"] = "Thanh toán không tồn tại";
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["Message"] = "Lỗi hệ thống, vui lòng thử lại";
            }
            
            return RedirectToAction("ListPayment");
        }

        [Authorize(Roles = "Staff")]
        [HttpGet("/list-ticket")]
        public IActionResult ListTicket(int? page)
        {
            int PageNumber = (page ?? 1);
            var list = ticketDAO.listTicketByStaff(Int32.Parse(User.FindFirstValue("Id")), PageNumber);
            if (list.PageCount != 0 && list.PageCount < PageNumber)
            {
                TempData["Message"] = "Sô trang không hợp lệ";
                return Redirect("/list-ticket");
            }
            return View(list);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("/list-ticket/update")]
        public IActionResult UpdateTicket()
        {
            try
            {
                var ticket = ticketDAO.ticketDetail(Int32.Parse(Request.Form["id"].ToString()));
                if (ticket != null 
                    && ticket.Status == (int)TicketStatus.Opening 
                    && ticket.StaffId == Int32.Parse(User.FindFirstValue("Id"))
                    )
                {
                    ticket.Status = Byte.Parse(Request.Form["status"].ToString());
                    ticketDAO.update(ticket);
                    TempData["Message"] = "Cập nhật thành công";
                }
                else
                {
                    TempData["Message"] = "Yêu cầu hỗ trợ không tồn tại";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi hệ thống, vui lòng thử lại";
            }

            return RedirectToAction("listTicket");
        }       

        [Authorize(Roles = "Staff")]
        [HttpGet("list-ticket/{id}")]
        public IActionResult TicketDetail(int id)
        {
            var ticket = ticketDAO.ticketDetail(id);
            if (ticket == null || ticket.StaffId != Int32.Parse(User.FindFirstValue("Id")))
            {
                TempData["Message"] = "Yêu cầu hỗ trợ không tồn tại";
                return RedirectToAction("listTicket");
            }
            ViewData["Ticket"] = ticket;
            ViewData["IdStaff"] = User.FindFirstValue("Id");
            return View();
        }

        [Authorize(Roles = "Staff")]
        [HttpPost]
        [Route("staff/reply")]
        public IActionResult reply([FromForm] TicketCommentDataModel commentData)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Vui lòng kiểm tra lại thông tin";
                    return RedirectToAction("TicketDetail", "Staff", new { Id = commentData.TicketId });
                }
                var ticket = ticketDAO.ticketDetail(commentData.TicketId);
                var idStaff = Int32.Parse(User.FindFirstValue("Id"));
                if (ticket == null || ticket.StaffId != idStaff)
                {
                    TempData["Message"] = "Yêu cầu hỗ trợ không tồn tại";
                    return RedirectToAction("listTicket");
                }
                else
                {
                    TicketComment commentInsert = new TicketComment
                    {
                        UserId = idStaff,
                        Comment = commentData.Comment,
                        TicketId = commentData.TicketId,
                    };
                    ticketDAO.insertComment(commentInsert);
                    TempData["Message"] = "Trả lời thành công";
                    if (commentData.IsClosed == true)
                    {
                        ticket.Status = (byte)TicketStatus.Closed;
                        ticketDAO.update(ticket);
                        TempData["Message"] = "Đóng yêu cầu hỗ trợ thành công";
                    }
                }
                return RedirectToAction("TicketDetail", "Staff", new { Id = commentData.TicketId });
            } catch (Exception ex)
            {
                TempData["Message"] = "Lỗi hệ thống, xin vui lòng thử lại";
                return RedirectToAction("listTicket");
            }
            
        }

        [HttpGet]
        [Route("manage-auction")]
        [Authorize(Roles = "Staff")]
        public IActionResult ManageAuction(int? pageNumber)
        {
            //get current user id
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (pageNumber.HasValue)
            {
                pagination.PageNumber = pageNumber.Value;
            }

            //get auction by user id
            List<Auction> auctions = auctionDAO.GetAuctionByUserId(userId, pagination);

            int auctionCount = auctionDAO.CountAuctionByUserId(userId);
            int pageSize = (int)Math.Ceiling((double)auctionCount / pagination.RecordPerPage);

            ViewBag.currentPage = pagination.PageNumber;
            ViewBag.pageSize = pageSize;

            return View(auctions);
        }

        [HttpGet]
        [Authorize(Roles = "Staff")]
        [Route("create-auction")]
        public IActionResult CreateAuction()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Staff")]
        [Route("create-auction")]
        public IActionResult CreateAuction([FromForm] AuctionDataModel auctionData)
        {
            //if value of user enter is not valid
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Vui lòng kiểm tra lại thông tin";
                ValidateAuction(auctionData);
                return View();
            }
            //if value of user enter is valid
            else
            {
                //validate the value of user enter
                bool validateModel = ValidateAuction(auctionData);
                if (validateModel)
                {
                    //get user by id
                    int userId = Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    User user = userDAO.GetUserById(userId);

                    auctionData.UserId = user.Id;
                    auctionData.Status = 1;
                    auctionData.Status = (int)AuctionStatus.Chờ_phê_duyệt;
                    auctionData.DeleteFlag = false;
                    auctionData.CreatedTime = DateTime.Now;


                    //create list Image
                    List<Image> images = new List<Image>();

                    foreach (var file in auctionData.ImageFiles)
                    {
                        //save image to folder and get url
                        var pathImage = FileUpload.UploadImageProduct(file);
                        if (pathImage != null)
                        {
                            Image image = new Image();
                            image.Url = pathImage;
                            images.Add(image);
                        }
                    }
                    //assign image to auction
                    auctionData.Images = images;

                    //map to Auction model
                    Auction auction = _mapper.Map<AuctionDataModel, Auction>(auctionData);
                    //add Auction to database

                    bool isSuccess = auctionDAO.AddAuction(auction);

                    //check if add acution successfull
                    if (isSuccess)
                    {
                        TempData["Message"] = "Tạo phiên đấu giá thành công!";

                        return Redirect("manage-auction");
                    }
                    else
                    {
                        TempData["Message"] = "Tạo phiên đấu giá thất bại!";
                        return View();
                    }
                }
                else
                {
                    TempData["Message"] = "Vui lòng kiểm tra lại thông tin";
                    return View();
                }
            }
        }


        [HttpGet]
        [Authorize(Roles = "Staff")]
        [Route("edit-auction")]
        public IActionResult EditAuction(int id)
        {
            //get current user id
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            //Find Auction by id
            Auction auction = auctionDAO.GetAuctionById(id);

            if (DateTime.Now.CompareTo(auction.StartTime) > 0)
            {
                TempData["Message"] = "Không thể chỉnh sửa phiên đấu giá đã bắt đầu!";
                return Redirect("manage-auction");
            }

            AuctionEditDataModel auctionData = _mapper.Map<Auction, AuctionEditDataModel>(auction);
            //check if auction belong to this user
            if (!(auction.UserId == userId))
            {
                TempData["Message"] = "Bạn không thể quản lý phiên đấu giá người khác!";
                return Redirect("manage-auction");
            }

            return View(auctionData);
        }

        [HttpGet]
        [Authorize(Roles = "Staff")]
        [Route("my-auction-details")]
        public IActionResult MyAuctionDetails(int id)
        {
            //get current user id
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            //Find Auction by id
            Auction? auction = auctionDAO.GetAuctionById(id);

            //Check if auction is exist
            if (auction == null)
            {
                TempData["Message"] = "Không tìm thấy phiên đấu giá này!";
                return Redirect("manage-auction");
            }

            //check if auction belong to this user
            if (!(auction.UserId == userId))
            {
                TempData["Message"] = "Bạn không thể xem chi tiết phiên đấu giá người khác!";
                return Redirect("manage-auction");
            }

            AuctionEditDataModel auctionData = _mapper.Map<Auction, AuctionEditDataModel>(auction);

            return View(auctionData);
        }

        [HttpPost]
        [Route("edit-auction")]
        public IActionResult EditAuction([FromForm] AuctionEditDataModel auctionData)
        {

            //get auction by id
            Auction auction = auctionDAO.GetApprovedAuction(auctionData.Id.Value);
            if (auction != null)
            {
                if (DateTime.Now.CompareTo(auction.StartTime) > 0)
                {
                    TempData["Message"] = "Không thể chỉnh sửa phiên đấu giá đã bắt đầu!";
                    return Redirect("manage-auction");
                }

                //if value of user enter is not valid
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Vui lòng kiểm tra lại thông tin";
                    ValidateAuction(auctionData);

                    auctionData.Images = auction.Images;
                    return View(auctionData);
                }
                //if value of user enter is valid
                else
                {
                    //validate the value of user enter
                    bool validateModel = ValidateAuction(auctionData);
                    if (validateModel)
                    {

                        //update status of auction
                        auctionData.Status = (int)AuctionStatus.Chờ_phê_duyệt;
                        auctionData.UpdatedTime = DateTime.Now;

                        //update new information
                        auction.Title = auctionData.Title;
                        auction.StartPrice = auctionData.StartPrice;
                        auction.EndPrice = auctionData.EndPrice;
                        auction.Area = auctionData.Area;
                        auction.Address = auctionData.Address;
                        auction.Facade = auctionData.Facade;
                        auction.Direction = auctionData.Direction;
                        auction.StartTime = auctionData.StartTime;
                        auction.EndTime = auctionData.EndTime;

                        auction.Status = auctionData.Status.Value;
                        auction.UpdatedTime = auctionData.UpdatedTime;


                        //check user update image or not
                        if (!auctionData.ImageFiles.IsNullOrEmpty())
                        {
                            //create list Image
                            List<Image> images = new List<Image>();

                            foreach (var file in auctionData.ImageFiles)
                            {
                                //save image to folder and get url
                                var pathImage = FileUpload.UploadImageProduct(file);
                                if (pathImage != null)
                                {
                                    Image image = new Image();
                                    image.Url = pathImage;
                                    images.Add(image);
                                }
                            }
                            auction.Images = images;
                        }

                        //update Auction to database
                        bool isSuccess = auctionDAO.EditAuction(auction);

                        //check if add acution successfull
                        if (isSuccess)
                        {
                            TempData["Message"] = "Cập nhật phiên đấu giá thành công!";
                            return Redirect("manage-auction");
                        }
                        else
                        {
                            TempData["Message"] = "Cập nhật phiên đấu giá thất bại!";

                            auctionData.Images = auction.Images;
                            return View(auctionData);
                        }
                    }
                    else
                    {
                        TempData["Message"] = "Vui lòng kiểm tra lại thông tin";

                        auctionData.Images = auction.Images;
                        return View(auctionData);
                    }
                }
            }
            TempData["Message"] = "Cập nhật phiên đấu giá thất bại!";

            return Redirect("manage-auction");
        }


        [HttpGet]
        [Route("delete-auction")]
        public IActionResult DeleteAuction(int id)
        {
            //get current user id
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            //Find Auction by id
            Auction auction = auctionDAO.GetApprovedAuction(id);

            //check if auction belong to this user
            if (auction != null)
            {
                if (!(auction.UserId == userId))
                {
                    TempData["Message"] = "Bạn không thể xóa phiên đấu giá người khác!";
                }
                else
                {
                    bool flag = auctionDAO.DeleteAuction(auction);
                    TempData["Message"] = flag ? "Xoá đấu giá thành công!" : "Xoá đấu giá thất bại!";
                }
            }
            else
            {
                TempData["Message"] = "Xoá đấu giá thất bại!";
            }


            return Redirect("manage-auction");
        }

        private bool ValidateAuction(AuctionDataModel auctionData)
        {
            var flag = true;

            //validate the end price with start price
            if (auctionData.EndPrice < auctionData.StartPrice)
            {
                ModelState.AddModelError("EndPrice", "Giá kết thúc phải lớn hơn giá khởi điểm");
                flag = false;
            }

            //validate the start time
            if (auctionData.StartTime.CompareTo(DateTime.Now) < 0)
            {
                ModelState.AddModelError("StartTime", "Thời gian bắt đầu phải sau thời điểm hiện tại!");
                flag = false;
            }

            //validate the end time
            if (auctionData.EndTime.CompareTo(auctionData.StartTime) < 0)
            {
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời điểm hiện tại!");
                flag = false;
            }

            return flag;
        }

        private bool ValidateAuction(AuctionEditDataModel auctionData)
        {
            var flag = true;

            //validate the end price with start price
            if (auctionData.EndPrice < auctionData.StartPrice)
            {
                ModelState.AddModelError("EndPrice", "Giá kết thúc phải lớn hơn giá khởi điểm");
                flag = false;
            }

            //validate the start time
            if (auctionData.StartTime.CompareTo(DateTime.Now) < 0)
            {
                ModelState.AddModelError("StartTime", "Thời gian bắt đầu phải sau thời điểm hiện tại!");
                flag = false;
            }

            //validate the end time
            if (auctionData.EndTime.CompareTo(auctionData.StartTime) < 0)
            {
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời điểm hiện tại!");
                flag = false;
            }

            return flag;
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

        [HttpGet("list-joining")]
        [Authorize(Roles = "Staff")]
        public IActionResult ListJoining(int? auctionId, int? pageNumber)
        {
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

            if (pageNumber.HasValue)
            {
                pagination.PageNumber = pageNumber.Value;
            }

            //Get list of participant
            var list = auctionBiddingDAO.GetJoiningByAuctionId(auctionId.Value, pagination);

            //Count number of participant
            int participantCount = auctionBiddingDAO.CountJoining(auctionId.Value);

            //Get number of page
            int pageSize = (int)Math.Ceiling((double)participantCount / pagination.RecordPerPage);

            ViewBag.currentPage = pagination.PageNumber;
            ViewBag.pageSize = pageSize;

            return View(list);
        }
    }
}
