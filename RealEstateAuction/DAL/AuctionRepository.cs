using Microsoft.EntityFrameworkCore;
using RealEstateAuction.DataModel;
using RealEstateAuction.Enums;
using RealEstateAuction.Models;
using X.PagedList;

namespace RealEstateAuction.DAL
{
    public class AuctionRepository
    {
        private readonly RealEstateContext context;
        public AuctionRepository()
        {
            context = new RealEstateContext();
        }

        public List<Auction> GetAuctionRecently(int number)
        {
            return context.Auctions
                .Where(a => a.Status == (int)AuctionStatus.Chấp_nhân
                || a.Status == (int)AuctionStatus.Kết_thúc
                && a.DeleteFlag == false
                && DateTime.Now <= a.EndTime)
                .Include(a => a.Images)
                .Include(a => a.User)
                .Include(a => a.Users)
                .OrderByDescending(a => a.CreatedTime)
                .Take(number)
                .ToList();
        }

    }
}
