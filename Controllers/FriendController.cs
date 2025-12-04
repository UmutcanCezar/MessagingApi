using api1.Data;
using api1.Dtos;
using api1.Entities;
using api1.Hubs; // 🔹 ChatHub için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FriendController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FriendController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetFriends(int userId)
        {
            var friends = await _context.Friends
                .Where(f => (f.UserID == userId) && f.Status == 1)
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .ToListAsync();

            var friendDtos = friends.Select(f =>
            {
                var otherUser = f.UserID == userId ? f.FriendUser : f.User;

                var lastMessage = _context.Messages
                    .Where(m => (m.SenderID == userId && m.ReceiverID == otherUser.Id) ||
                                (m.SenderID == otherUser.Id && m.ReceiverID == userId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                return new FriendDto
                {
                    FriendId = otherUser.Id,
                    Username = otherUser.Username,
                    EmailAddress = otherUser.EmailAddress,
                    ProfilePictureUrl = otherUser.ProfilePictureUrl,
                    LastMessage = lastMessage?.Content,
                    LastMessageTime = lastMessage?.SentAt,
                    LastMessageSenderId = lastMessage?.SenderID,
                    CreatedAt = f.CreatedAt,
                    IsOnline = ChatHub.IsUserOnline(otherUser.Id) // 🔹 online/offline durumu
                };
            })
            .OrderByDescending(f => f.LastMessageTime ?? DateTime.MinValue)
            .ToList();

            return Ok(friendDtos);
        }

        [HttpGet("notfriends/{userId}")]
        public async Task<IActionResult> GetNotFriends(int userid)
        {
            var allusers = _context.Users.Where(u => u.Id != userid);
            var alreadyReceiveRequest = _context.FriendRequests.Where(u => u.SenderId == userid).Select(u => u.ReceiverId);
            var alreadySendRequest = _context.FriendRequests.Where(u => u.ReceiverId == userid).Select(u => u.SenderId);

            var FriednsID = await _context.Friends
                .Where(f => (f.UserID == userid || f.FriendID == userid) && f.Status == 1)
                .Select(f => f.UserID == userid ? f.FriendID : f.UserID)
                .ToListAsync();

            var NotFriends = await allusers
                .Where(u => !FriednsID.Contains(u.Id) && !alreadyReceiveRequest.Contains(u.Id) && !alreadySendRequest.Contains(u.Id))
                .Select(u => new FriendDto
                {
                    FriendId = u.Id,
                    Username = u.Username,
                    EmailAddress = u.EmailAddress,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    CreatedAt = u.CreadetAt,
                    IsOnline = ChatHub.IsUserOnline(u.Id) // 🔹 online/offline durumu
                }).ToListAsync();

            return Ok(NotFriends);
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFriend(int userid, int friendID)
        {
            var f1 = await _context.Friends.FirstOrDefaultAsync(f => f.UserID == userid && f.FriendID == friendID);
            var f2 = await _context.Friends.FirstOrDefaultAsync(f => f.UserID == friendID && f.FriendID == userid);
            if (f1 != null) _context.Remove(f1);
            if (f2 != null) _context.Remove(f2);
            await _context.SaveChangesAsync();
            return Ok("Arkadaşlık silindi.");
        }
    }
}
