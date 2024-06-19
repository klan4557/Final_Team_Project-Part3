using ASP_ChatApp_test1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace ASP_ChatApp_test1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static readonly HashSet<string> activeRooms = new HashSet<string>();
        private static readonly ConcurrentDictionary<string, List<string>> chatLogs = new ConcurrentDictionary<string, List<string>>();
        private static readonly ConcurrentDictionary<string, bool> roomUsers = new ConcurrentDictionary<string, bool>();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Chat(string room)
        {
            if (string.IsNullOrEmpty(room))
            {
                return RedirectToAction("Index");
            }

            ViewBag.Room = room;
            ViewBag.ChatLogs = chatLogs.ContainsKey(room) ? chatLogs[room] : new List<string>();
            ViewBag.IsAdmin = false;
            return View();
        }

        public IActionResult Admin()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GoChat()
        {
            var remoteIpAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            if (remoteIpAddress == "::1" || remoteIpAddress == "127.0.0.1") // 관리자의 IP 주소를 확인
            {
                return RedirectToAction("Admin");
            }
            else
            {
                // 사용자를 비어 있는 채팅방으로 이동
                string[] rooms = { "Room1", "Room2", "Room3", "Room4", "Room5" };
                string emptyRoom = rooms.FirstOrDefault(r => !activeRooms.Contains(r));

                if (emptyRoom != null)
                {
                    activeRooms.Add(emptyRoom);
                    roomUsers[emptyRoom] = true;
                    return RedirectToAction("Chat", new { room = emptyRoom });
                }
                else
                {
                    // 모든 방이 꽉 찬 경우 처리 (원하는 대로 처리)
                    return Content("No rooms available. Please try again later.");
                }
            }
        }

        [HttpGet]
        public IActionResult AdminChat(string room)
        {
            if (string.IsNullOrEmpty(room))
            {
                return Json(new { success = false, message = "No user in this room." });
            }

            if (!roomUsers.ContainsKey(room) || !roomUsers[room])
            {
                return Json(new { success = false, message = "No user in this room." });
            }

            ViewBag.Room = room;
            ViewBag.ChatLogs = chatLogs.ContainsKey(room) ? chatLogs[room] : new List<string>();
            ViewBag.IsAdmin = true;
            return Json(new { success = true, url = Url.Action("AdminChatRoom", new { room }) });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult LeaveRoom(string room)
        {
            if (!string.IsNullOrEmpty(room))
            {
                activeRooms.Remove(room);
                roomUsers[room] = false;
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult SaveMessage(string room, string user, string message)
        {
            if (string.IsNullOrEmpty(room))
            {
                return BadRequest("Room name cannot be null or empty.");
            }

            var logMessage = $"{user}: {message}";
            if (!chatLogs.ContainsKey(room))
            {
                chatLogs[room] = new List<string>();
            }
            chatLogs[room].Add(logMessage);
            return Ok();
        }

        [HttpPost]
        public IActionResult EndChat(string room)
        {
            if (!string.IsNullOrEmpty(room))
            {
                activeRooms.Remove(room);
                chatLogs.TryRemove(room, out _);
                roomUsers[room] = false;
            }
            return Ok();
        }

        [HttpGet]
        public IActionResult AdminChatRoom(string room)
        {
            ViewBag.Room = room;
            ViewBag.ChatLogs = chatLogs.ContainsKey(room) ? chatLogs[room] : new List<string>();
            ViewBag.IsAdmin = true;
            return View("Chat");
        }
    }
}
