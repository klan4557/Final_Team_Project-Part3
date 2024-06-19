using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP_ChatApp_test1.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> RoomAdmins = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, List<string>> RoomMessages = new ConcurrentDictionary<string, List<string>>();

        public async Task JoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            var user = Context.User.Identity.Name == "Admin" ? "Admin" : Context.User.Identity.Name;
            if (user == "Admin")
            {
                RoomAdmins[roomName] = Context.ConnectionId;
                await Clients.Group(roomName).SendAsync("AdminJoined");

                if (RoomMessages.ContainsKey(roomName))
                {
                    foreach (var message in RoomMessages[roomName])
                    {
                        var parts = message.Split(':');
                        var sender = parts[0];
                        var text = string.Join(":", parts.Skip(1));
                        await Clients.Caller.SendAsync("ReceiveMessage", sender, text);
                    }
                }
            }
            else
            {
                if (!RoomMessages.ContainsKey(roomName))
                {
                    RoomMessages[roomName] = new List<string>();
                }

                await Clients.Group(roomName).SendAsync("UserJoined");

                if (RoomAdmins.TryGetValue(roomName, out var adminConnectionId))
                {
                    await Clients.Client(adminConnectionId).SendAsync("UserJoinedNotification");
                }

                foreach (var message in RoomMessages[roomName])
                {
                    var parts = message.Split(':');
                    var sender = parts[0];
                    var text = string.Join(":", parts.Skip(1));
                    await Clients.Caller.SendAsync("ReceiveMessage", sender, text);
                }
            }
        }

        public async Task SendMessage(string roomName, string user, string message)
        {
            var fullMessage = $"{user}:{message}";
            if (!RoomMessages.ContainsKey(roomName))
            {
                RoomMessages[roomName] = new List<string>();
            }
            RoomMessages[roomName].Add(fullMessage);

            await Clients.Group(roomName).SendAsync("ReceiveMessage", user, message);
        }

        public async Task LeaveRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }
    }
}
