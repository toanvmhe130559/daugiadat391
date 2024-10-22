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

        [Authorize(Roles = "Member")]
        [HttpGet("member/list-ticket")]
        public IActionResult ListTicketUser(int? page)
        {
            int PageNumber = (page ?? 1);
            var list = ticketDAO.listTicketByUser(Int32.Parse(User.FindFirstValue("Id")), PageNumber);
            if (list.PageCount != 0 && list.PageCount < PageNumber)
            {
                TempData["Message"] = "Sô trang không hợp lệ";
                return Redirect("member/list-ticket");
            }
            ViewData["List"] = list;
            return View();
        }
        [HttpGet("join-auction")]
        public IActionResult JoinAuction(int auctionId)
        {
            //get current user id
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            //find user by id
            User user = userDAO.GetUserById(userId);

            //Find Auction by id
            Auction auction = auctionDAO.GetAuctionById(auctionId);

            //Check auction is exist
            if (auction == null)
            {
                TempData["Message"] = "Phiên đấu giá không tồn tại!";
                return Redirect("manage-auction");
            }

            //check if auction belong to this user
            if (auction.UserId == userId)
            {
                TempData["Message"] = "Bạn không thể tham gia đấu giá phiên đấu giá của mình!";
                return Redirect("/auction-details?auctionId=" + auctionId);
            }

            if (DateTime.Now < auction.StartTime)
            {
                TempData["Message"] = "Phiên đấu giá chưa bắt đầu!";
                return Redirect("/auction-details?auctionId=" + auctionId);
            }

            //check if auction is expired
            if (auction.EndTime.CompareTo(DateTime.Now) < 0 || auction.Status == (int)AuctionStatus.Kết_thúc)
            {
                TempData["Message"] = "Phiên đấu giá đã kết thúc!";
                return Redirect("/auction-details?auctionId=" + auctionId);
            }

            //check if user has joined auction
            if (auctionDAO.IsUserJoinedAuction(user, auctionId))
            {
                TempData["Message"] = "Bạn đã tham gia đấu giá!";
                return Redirect("/auction-details?auctionId=" + auctionId);
            }

            //add new user to list user join auction
            auction.Users.Add(user);

            //update Auction to database
            bool isSuccess = auctionDAO.EditAuction(auction);

            //check if join acution successfull
            if (isSuccess)
            {
                TempData["Message"] = "Tham gia đấu giá thành công!";
            }
            else
            {
                TempData["Message"] = "Tham gia đấu giá thất bại!";
            }

            return Redirect("/auction-details?auctionId=" + auctionId);
        }
        [HttpPost("bidding-auction")]
        [Authorize(Roles = "Member")]
        public IActionResult Bidding(BiddingDataModel biddingDataModel)
        {
            //Get current url
            string url = Request.Headers["Referer"];

            //get user by id
            User user = userDAO.GetUserById(biddingDataModel.MemberId);

            //Get Auction by id
            Auction? auction = auctionDAO.GetAuctionById(biddingDataModel.AuctionId);


            //Check if auction is exist
            if (auction == null)
            {
                TempData["Message"] = "Phiên đấu giá không tồn tại!";
                return Redirect(url);
            }

            //Check if auction is started
            if (auction.StartTime > DateTime.Now)
            {
                TempData["Message"] = "Phiên đấu giá chưa bắt đầu!";
                return Redirect(url);
            }

            //Check if auction is expired
            if (auction.EndTime.CompareTo(DateTime.Now) < 0 || auction.Status == (int)AuctionStatus.Kết_thúc)
            {
                TempData["Message"] = "Phiên đấu giá đã kết thúc!";
                return Redirect(url);
            }

            //Check user has joined auction
            bool isJoined = auctionDAO.IsUserJoinedAuction(user, biddingDataModel.AuctionId);
            if (!isJoined)
            {
                TempData["Message"] = "Bạn chưa tham gia đấu giá!";
                return Redirect(url);
            }

            //Check if bidding price is greater than start price
            if (biddingDataModel.BiddingPrice < auction.StartPrice)
            {
                TempData["Message"] = "Giá đấu phải lớn hơn giá khởi điểm!";
                return Redirect(url);
            }

            //check if bidding price is greater than end price
            if (biddingDataModel.BiddingPrice > auction.EndPrice)
            {
                TempData["Message"] = "Giá đấu phải nhỏ hơn giá kết thúc!";
                return Redirect(url);
            }

            //Get list bidding of auction
            List<AuctionBidding> auctionBiddings = auctionBiddingDAO.GetAuctionBiddings(biddingDataModel.AuctionId);

            //Check bidding price of participant is greater than the max price
            if (auctionBiddings.Count > 0)
            {
                decimal maxPrice = auctionBiddings.Max(ab => ab.BiddingPrice);
                if (biddingDataModel.BiddingPrice <= maxPrice)
                {
                    TempData["Message"] = "Giá đấu phải lớn hơn giá đấu cao nhất hiện tại!";
                    return Redirect(url);
                }
            }

            //Get the last bidding of user for this auction
            AuctionBidding? lastBidding = auctionBiddingDAO.GetLastBiddingByUser(biddingDataModel.AuctionId, biddingDataModel.MemberId);

            //If bidding price is equal to end price
            if (biddingDataModel.BiddingPrice == auction.EndPrice)
            {
                //Update status of auction to end
                auction.Status = (int)AuctionStatus.Kết_thúc;

                //Update Auction to database
                auctionDAO.EditAuction(auction);
            }
            //If bidding price is valid add to database
            //Map to AuctionBidding model
            AuctionBidding auctionBidding = _mapper.Map<BiddingDataModel, AuctionBidding>(biddingDataModel);
            auctionBidding.TimeBidding = DateTime.Now;

            bool isSuccess = auctionBiddingDAO.AddAuctionBidding(auctionBidding);
            Auction auctionEnd = auctionDAO.GetAuctionEndById(biddingDataModel.AuctionId);

            //Check user bidding successfull
            if (isSuccess)
            {
                TempData["Message"] = "Đấu giá thành công!";
            }
            else
            {
                TempData["Message"] = "Có lỗi xảy ra khi đặt giá!";
            }

            return Redirect(url);
        }
        [HttpGet("/top-up")]
        [Authorize(Roles = "Member")]
        public IActionResult TopUp(int? page)
        {
            int PageNumber = (page ?? 1);
            ViewData["List"] = paymentDAO.listByUserId(Int32.Parse(User.FindFirstValue("Id")), PageNumber);

            ViewData["Banks"] = bankDAO.listBankings();

            return View();
        }
        [HttpPost("/top-up-post")]
        [Authorize(Roles = "Member")]
        public IActionResult TopUpPost([FromForm] PaymentDataModel paymentData)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Payment payment;
                    switch (paymentData.Action)
                    {
                        case PaymentType.TopUp:
                            payment = new Payment()
                            {
                                BankId = paymentData.BankId,
                                Amount = paymentData.Amount,
                                UserBankName = paymentData.UserBankName,
                                UserBankAccount = paymentData.UserAccountNumber,
                                Code = $"NAP_{DateTime.Now.ToShortTimeString()}",
                                TransactionDate = DateTime.Now,
                                Status = (int)PaymentStatus.Pending,
                                UserId = Int32.Parse(User.FindFirstValue("Id")),
                                Type = (byte)paymentData.Action,
                            };
                            paymentDAO.insert(payment);
                            payment.Bank = bankDAO.bankDetail(paymentData.BankId);

                            break;
                        case PaymentType.Withdraw:
                            var user = userDAO.GetUserById(Int32.Parse(User.FindFirstValue("Id")));
                            Console.Write(user.Id);
                            if (user.Wallet < paymentData.Amount)
                            {
                                TempData["Message"] = "Không thể tạo yêu cầu do số tiền rút cao hơn số tiền trong ví";

                                return RedirectToAction("TopUp");
                            }
                            payment = new Payment()
                            {
                                Amount = paymentData.Amount,
                                UserBankAccount = paymentData.UserAccountNumber,
                                UserBankName = paymentData.UserBankName,
                                Code = $"RUT_{DateTime.Now.ToShortTimeString()}",
                                TransactionDate = DateTime.Now,
                                Status = (int)PaymentStatus.Pending,
                                UserId = Int32.Parse(User.FindFirstValue("Id")),
                                Type = (byte)paymentData.Action,
                            };
                            paymentDAO.insert(payment);

                            break;
                        case PaymentType.Refund:
                            int paymentId = Int32.Parse(Request.Form["PaymentId"]);
                            var paymentRefund = paymentDAO.getPaymentRefund(paymentId);
                            var newPayment = new Payment()
                            {
                                Amount = paymentRefund.Amount,
                                UserBankAccount = paymentRefund.UserBankAccount,
                                UserBankName = paymentRefund.UserBankName,
                                Code = $"HOAN_{DateTime.Now.ToShortTimeString()}",
                                TransactionDate = DateTime.Now,
                                Status = (int)PaymentStatus.Pending,
                                UserId = Int32.Parse(User.FindFirstValue("Id")),
                                Type = (byte)paymentData.Action,
                            };
                            paymentDAO.insert(newPayment);
                            break;
                    }
                    TempData["Message"] = "Tạo yêu cầu thành công";
                }
                else
                {
                    Console.WriteLine(1);
                    foreach (var modelStateEntry in ModelState.Values)
                    {
                        foreach (var error in modelStateEntry.Errors)
                        {
                            Console.WriteLine(error.ErrorMessage);
                        }
                    }
                    TempData["Message"] = "Tạo yêu cầu thất bại";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(2);
                Console.WriteLine(ex.Message);

                TempData["Message"] = "Tạo yêu cầu thất bại";
            }

            return RedirectToAction("TopUp");
        }

        [Authorize(Roles = "Member")]
        [HttpGet("member/list-ticket")]
        public IActionResult ListTicketUser(int? page)
        {
            int PageNumber = (page ?? 1);
            var list = ticketDAO.listTicketByUser(Int32.Parse(User.FindFirstValue("Id")), PageNumber);
            if (list.PageCount != 0 && list.PageCount < PageNumber)
            {
                TempData["Message"] = "Sô trang không hợp lệ";
                return Redirect("member/list-ticket");
            }
            ViewData["List"] = list;
            return View();
        }

        [Authorize(Roles = "Member")]
        [HttpGet("/member/list-ticket/{id}")]
        public IActionResult TicketDetailUser(int id)
        {
            var ticket = ticketDAO.ticketDetail(id);
            if (ticket == null || ticket.UserId != Int32.Parse(User.FindFirstValue("Id")))
            {
                TempData["Message"] = "Yêu cầu hỗ trợ không tồn tại";
                return RedirectToAction("ListTicket");
            }
            ViewData["Ticket"] = ticket;
            ViewData["IdUser"] = User.FindFirstValue("Id");
            return View();
        }

        [Authorize(Roles = "Member")]
        [HttpPost]
        [Route("member/reply")]
        public IActionResult ReplyUser([FromForm] TicketCommentDataModel commentData)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Message"] = "Vui lòng kiểm tra lại thông tin";
                    return RedirectToAction("TicketDetail", "Account", new { Id = commentData.TicketId });
                }
                var ticket = ticketDAO.ticketDetail(commentData.TicketId);
                var idUser = Int32.Parse(User.FindFirstValue("Id"));
                if (ticket == null || ticket.UserId != idUser)
                {
                    TempData["Message"] = "Yêu cầu hỗ trợ không tồn tại";
                    return RedirectToAction("listTicket");
                }
                else
                {
                    TicketComment commentInsert = new TicketComment
                    {
                        UserId = idUser,
                        Comment = commentData.Comment,
                        TicketId = commentData.TicketId,
                    };
                    ticketDAO.insertComment(commentInsert);
                    TempData["Message"] = "Trả lời thành công";
                }
                return RedirectToAction("TicketDetailUser", "Account", new { Id = commentData.TicketId });
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi hệ thống, xin vui lòng thử lại";
                return RedirectToAction("ListTicket");
            }
        }
    }
}
