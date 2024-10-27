using Microsoft.AspNetCore.Mvc.Routing;
using RealEstateAuction.DAL;
using RealEstateAuction.DataModel;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;

namespace RealEstateAuction.Services
{
    public class TimerService
    {
        private readonly ILogger _logger;
        private readonly AuctionDAO _auctionDAO;
        private readonly UserDAO _userDAO;
        private readonly IUrlHelperFactory _urlHelperFactory;

        public TimerService(ILogger logger, IUrlHelperFactory urlHelperFactory)
        {
            _logger = logger;
            _auctionDAO = new AuctionDAO();
            _userDAO = new UserDAO();
            _urlHelperFactory = urlHelperFactory;
        }

        //Shedules a task to be executed at a specific time
        public void EndAuction(List<Auction> ending)
        {
            if (!ending.Any()) // Check if the list is empty
            {
                return; // If the list is empty, return without doing anything
            }

            // Loop through the list of auctions
            foreach (var auction in ending)
            {
                TimeSpan ts = new TimeSpan(0);
                // Try catch to ignore Zero or negative value
                try
                {
                    // Calculate the time left until the auction ends
                    ts = auction.EndTime - DateTime.Now;
                }
                catch (Exception ex)
                {
                    ts = TimeSpan.FromSeconds(2);
                }

                // Initialize the timer
                Timer timer = InitializeTimer(auction.Id, ts);
            }
        }

        private void DoEndAuction(int auctionId, Timer timer)
        {
            timer.Dispose();

            //get auction by id
            Auction auction = _auctionDAO.GetAuctionEndById(auctionId);
            //update status of auction to ended
            auction.Status = (int)AuctionStatus.Kết_thúc;
            //update auction to database
            _auctionDAO.EditAuction(auction);
            if (auction.Users.Count > 0)
            {
                // Check if there is any bid
                if (auction.AuctionBiddings.Count == 0)
                {
                    // Return all fee to the participants
                    foreach (var user in auction.Users)
                    {
                        user.Wallet += Constant.Fee;
                        _userDAO.UpdateUser(user);
                    }
                }
                else
                {
                    // Get the highest bid
                    var highestBid = auction.AuctionBiddings.Max(x => x.BiddingPrice);

                    // Get the winner base on the highest bid
                    var winner = auction.AuctionBiddings.FirstOrDefault(x => x.BiddingPrice == highestBid).Member;

                    // Return the fee to the participants except the winner
                    foreach (var user in auction.Users)
                    {
                        if (user.Id != winner.Id)
                        {
                            // Get user by Id
                            var userById = _userDAO.GetUserById(user.Id);

                            // Update the wallet of the user
                            userById.Wallet += Constant.Fee;
                            _userDAO.UpdateUser(userById);
                        }
                    }
                }
            }
            _logger.LogInformation("Auction end at {time}", DateTime.Now);
        }

        private Timer InitializeTimer(int auctionId, TimeSpan ts)
        {
            Timer timer = null;
            // Create a new timer
            timer = new Timer(_ => DoEndAuction(auctionId, timer), null, ts, TimeSpan.FromSeconds(2));
            return timer;
        }
    }
}
