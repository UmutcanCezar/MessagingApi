using api1.Entities;
using Microsoft.AspNetCore.Mvc;

namespace api1.repository
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetMessagesAsync(int User1ID, int User2Id);
        Task SaveMessageAsync(Message message);
        Task<Message?> GetMessageByIdAsync(int messageId);
        Task SaveChangesAsync();
        Task<IEnumerable<Message>> GetSelfMessageAsync(int userId);
    }
}
