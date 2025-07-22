using Microsoft.AspNetCore.Mvc;
using TRKart.Entities.DTOs;
using TRKart.Business.Interfaces;

namespace TRKart.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserCardController : ControllerBase
    {
        private readonly IUserCardService _userCardService;

        public UserCardController(IUserCardService userCardService)
        {
            _userCardService = userCardService;
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetUserCards(int customerId)
        {
            var cards = await _userCardService.GetUserCardsAsync(customerId);
            return Ok(cards);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserCard([FromBody] UserCardDto dto)
        {
            var result = await _userCardService.CreateUserCardAsync(dto);
            if (!result)
                return BadRequest("Kart oluşturulamadı.");

            return Ok("Kart başarıyla oluşturuldu!");
        }
    }
}