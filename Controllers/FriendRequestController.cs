using api1.Data;
using api1.Dtos;
using api1.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FriendRequestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FriendRequestController(AppDbContext context)
        {
            _context = context;
        }

        // ✔ Arkadaşlık isteği gönderme
        [HttpPost("send")]
        public async Task<IActionResult> SendRequest(int senderId, int receiverId)
        {
            if (senderId == receiverId)
                return BadRequest("Kendinize istek gönderemezsiniz.");

            // ❗ Zaten arkadaş mı?
            bool alreadyFriends = await _context.Friends.AnyAsync(x =>
                (x.UserID == senderId && x.FriendID == receiverId) ||
                (x.UserID == receiverId && x.FriendID == senderId));

            if (alreadyFriends)
                return BadRequest("Zaten arkadaşsınız.");

            // ❗ Daha önce istek gönderilmiş mi?
            var existingRequest = await _context.FriendRequests.FirstOrDefaultAsync(x =>
                (x.SenderId == senderId && x.ReceiverId == receiverId) ||
                (x.SenderId == receiverId && x.ReceiverId == senderId));

            if (existingRequest != null)
            {
                // ❗ Karşılıklı istek ise otomatik arkadaş yap
                if (existingRequest.SenderId == receiverId)
                {
                    var f1 = new Friend { UserID = senderId, FriendID = receiverId, Status = 1 };
                    var f2 = new Friend { UserID = receiverId, FriendID = senderId, Status = 1 };

                    _context.Friends.AddRange(f1, f2);
                    _context.FriendRequests.Remove(existingRequest);
                    await _context.SaveChangesAsync();

                    return Ok("Karşılıklı istek tespit edildi. Artık arkadaşsınız.");
                }

                return BadRequest("Zaten bir istek mevcut.");
            }

            // ✔ Yeni istek oluştur
            var newRequest = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId
            };

            _context.FriendRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            return Ok(new { message = "İstek gönderildi." });

        }

        // ✔ İsteği reddet
        [HttpDelete("reject/{id}")]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var req = await _context.FriendRequests.FindAsync(id);
            if (req == null) return NotFound();

            _context.FriendRequests.Remove(req);
            await _context.SaveChangesAsync();

            return Ok("İstek reddedildi.");
        }

        // ✔ İsteği kabul et
        [HttpPost("accept/{id}")]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var req = await _context.FriendRequests.FindAsync(id);
            if (req == null) return NotFound();

            var f1 = new Friend { UserID = req.SenderId, FriendID = req.ReceiverId, Status = 1 };
            var f2 = new Friend { UserID = req.ReceiverId, FriendID = req.SenderId, Status = 1 };

            _context.Friends.AddRange(f1, f2);
            _context.FriendRequests.Remove(req);

            await _context.SaveChangesAsync();

            return Ok("İstek kabul edildi.");
        }

        // ✔ Gelen istekler
        [HttpGet("pending/{userId}")]
        public async Task<IActionResult> GetIncomingRequests(int userId)
        {
            var pending = await _context.FriendRequests
                .Where(r => r.ReceiverId == userId)
                .Include(r => r.Sender)
                .Select(r => new FriendDto
                {
                    RequestId = r.Id,
                    FriendId = r.SenderId,
                    Username = r.Sender.Username,
                    EmailAddress = r.Sender.EmailAddress,
                    ProfilePictureUrl = r.Sender.ProfilePictureUrl,
                    CreatedAt = r.Sender.CreadetAt
                })
                .ToListAsync();

            return Ok(pending);
        }
    }
}
