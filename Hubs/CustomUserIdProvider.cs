using Microsoft.AspNetCore.SignalR;
using System;

namespace api1.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            var httpContext = connection.GetHttpContext();
            var userId = httpContext.Request.Query["userId"].ToString();
            Console.WriteLine("CustomUserIdProvider: UserId = " + userId); // Log
            return userId;
        }
    }
}
