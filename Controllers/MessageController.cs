using api1.Data;
using api1.Entities;
using api1.repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace api1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;

    public MessageController(IMessageRepository repository)
    {
        _messageRepository = repository;
    }
    [HttpGet("{User1ID}/{User2Id}")]
    public async Task<IActionResult> GetMessages(int User1ID,int User2Id)
    {
        var messages = await _messageRepository.GetMessagesAsync(User1ID, User2Id);
        return Ok(messages);
    }
    [HttpGet("self/{userId}")]
    public async Task<IActionResult> GetSelfMessage(int userId)
    {
        var messages = await _messageRepository.GetSelfMessageAsync(userId);
            return Ok(messages);
    }

    [HttpPost]
    public async Task <IActionResult> SendMessage([FromBody]Message message)
    {
        await _messageRepository.SaveMessageAsync(message);
        return CreatedAtAction(
                            nameof(GetMessages),
                            new { User1ID = message.SenderID, User2Id = message.ReceiverID },
                            message
                                    );
    }
}
