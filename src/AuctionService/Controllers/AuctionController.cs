
using AuctionService.DTOs;
using Microsoft.AspNetCore.Mvc;
using  AuctionService.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AuctionService.Models;
using AutoMapper.QueryableExtensions;
using MassTransit;
using Contracts;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/auctions")]
    public class AuctionController:ControllerBase
    {
        private readonly AuctionDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuctionController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
            _context = context;
            _mapper = mapper;
                
        }
        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
        {
            var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

            if(!string.IsNullOrEmpty(date)) {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }
       

            return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
           
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid Id)
        {
           var auction = await _context.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == Id);
           
            if(auction == null) {
                return NotFound();
            }

            return _mapper.Map<AuctionDto>(auction);
           
        }

        [HttpPost]
        public async Task<ActionResult<List<AuctionDto>>> CreateAuction(CreateAuctionDto auctionDto)
        { 
            var auction = _mapper.Map<Auction>(auctionDto);

            auction.Seller = "test";

            _context.Auctions.Add(auction);

            var newAuction =_mapper.Map<AuctionDto>(auction);

            await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

            var result = await _context.SaveChangesAsync() > 0;

            if(!result) return BadRequest("Could not save changes to the DB");

            return CreatedAtAction(nameof(GetAuctionById), 
                    new {auction.Id}, newAuction);
           
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<List<AuctionDto>>> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        { 
            var auction = await _context.Auctions.Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if(auction == null) return NotFound();

            //TODO: check seller == username

            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;


            await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

            var result = await _context.SaveChangesAsync() > 0;



            if(result) return Ok();

            return BadRequest("Problem saving changes");
           
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<AuctionDto>> DeleteAuction(Guid Id)
        {
           var auction = await _context.Auctions.FindAsync(Id);
           
            if(auction == null) {
                return NotFound();
            }

            // TODO: check seller == username

            _context.Auctions.Remove(auction);

            await _publishEndpoint.Publish(_mapper.Map<AuctionDeleted>(new AuctionDeleted{Id = Id.ToString()}));

           var result = await _context.SaveChangesAsync() > 0;

            if(result) return Ok();

            return BadRequest("Could not update db");
           
        }

    }
}