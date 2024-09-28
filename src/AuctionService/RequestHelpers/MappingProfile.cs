
using AuctionService.DTOs;
using AuctionService.Models;
using AutoMapper;

namespace AuctionService.RequestHelpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Auction, AuctionDto>()
                .IncludeMembers(a => a.Item);
            CreateMap<Item, AuctionDto>();
            CreateMap<CreateAuctionDto, Auction>()
                .ForMember(d => d.Item, o => o.MapFrom(s => s));
            CreateMap<CreateAuctionDto, Item>();
        }
    }
}