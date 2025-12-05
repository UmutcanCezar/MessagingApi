using api1.Entities;
using api1.repository;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api1.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageRepository _messageRepository;

        // Online kullanıcılar merkezi
        private static readonly HashSet<int> OnlineUsers = new();

        public ChatHub(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        // Kullanıcı hub’a bağlandığında
        public override async Task OnConnectedAsync()
        {
            // Render USERID'yi UserIdentifier içine koyamıyor: Query’den manuel alıyoruz
            var httpContext = Context.GetHttpContext();
            var queryUserId = httpContext?.Request.Query["userId"].ToString();

            Console.WriteLine("Render Query userId = " + queryUserId);
            Console.WriteLine("Context.UserIdentifier BEFORE = " + Context.UserIdentifier);

            string finalUserId = Context.UserIdentifier ?? queryUserId;

            Console.WriteLine("Context.UserIdentifier FINAL = " + finalUserId);

            if (int.TryParse(finalUserId, out int userId))
            {
                OnlineUsers.Add(userId);
                await Clients.All.SendAsync("UpdateUserStatus", userId, true);
            }

            await base.OnConnectedAsync();
        }

        // Kullanıcı hub’dan çıktığında
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (int.TryParse(Context.UserIdentifier, out int userId))
            {
                OnlineUsers.Remove(userId);
                await Clients.All.SendAsync("UpdateUserStatus", userId, false);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // API’de kullanmak için online kontrolü
        public static bool IsUserOnline(int userId)
        {
            return OnlineUsers.Contains(userId);
        }

        // Mesaj gönderme
        public async Task SendMessage(int senderID, int receiverID, string message)
        {
            try
            {
                var msgEntity = new Message
                {
                    SenderID = senderID,
                    ReceiverID = receiverID,
                    Content = message,
                    SentAt = DateTime.UtcNow
                };

                await _messageRepository.SaveMessageAsync(msgEntity);

                await Clients.Users(senderID.ToString(), receiverID.ToString())
                    .SendAsync("ReceiveMessage", senderID, receiverID, message, msgEntity.SentAt);

                await Clients.Users(senderID.ToString(), receiverID.ToString())
                    .SendAsync("LastMessage", senderID, receiverID, message, msgEntity.SentAt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hub SendMessage hatası: " + ex);
                throw;
            }
        }
    }
}
