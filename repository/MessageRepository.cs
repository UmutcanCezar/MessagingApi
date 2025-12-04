using api1.Data;
using api1.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api1.repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;
        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Message>> GetMessagesAsync(int User1ID, int User2Id)
        {
            return await _context.Messages.Where(
                m => (m.SenderID == User1ID && m.ReceiverID == User2Id) ||
                (m.SenderID == User2Id && m.ReceiverID == User1ID)).OrderBy(m => m.SentAt).ToListAsync();

        }
        public async Task SaveMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }
        public async Task<Message?> GetMessageByIdAsync(int messageId)
        {
            return await _context.Messages.FindAsync(messageId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Message>> GetSelfMessageAsync(int userId)
        {
            return await _context.Messages.Where
                (m => m.SenderID == userId && m.ReceiverID == userId).OrderByDescending(m => m.SentAt).OrderBy(m => m.SentAt).ToListAsync();


        }
    }
}
