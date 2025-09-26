using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth.OAuth2;
using static System.Net.Mime.MediaTypeNames;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace PushNotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PushNotificationController : ControllerBase
    {
        private readonly NotificationService m_NotificationService;

        public PushNotificationController(NotificationService _service)
        {
            m_NotificationService = _service;
        }

        // POST api/PushNotification/register-token
        [HttpPost("register-token")]
        public IActionResult RegisterToken([FromBody] TokenRequest request)
        {
            if (!string.IsNullOrEmpty(request.m_Token))
            {
                m_NotificationService.RegisterToken(request.m_ID, request.m_Token);

                return Ok(new { message = "Token registered successfully" });
            }
            return BadRequest(new { error = "Invalid token" });
        }

        [HttpPost("deregister-token")]
        public IActionResult DeregisterToken([FromBody] TokenRequest request)
        {
            if (!string.IsNullOrEmpty(request.m_Token))
            {
                m_NotificationService.DeregisterToken(request.m_ID, request.m_Token);

                return Ok(new { message = "Token removed successfully" });
            }
            return BadRequest(new { error = "Invalid token" });
        }

        [HttpPost("push-notification")]
        public IActionResult PushNotification([FromBody] PushRequest request)
        {
            if (!string.IsNullOrEmpty(request.m_Image))
            {
                m_NotificationService.SendUser(request.m_UserID, request.m_Title, request.m_Message, request.m_Image, request.m_QueueNumber);

                return Ok(new { message = "Push Notification Successful" });
            }
            return BadRequest(new { error = "Push Notification Unsuccessful" });
        }

        [HttpGet("{queueNumber}")]
        public IActionResult GetQueueImage(int queueNumber, [FromQuery] string image, [FromQuery] string signiture)
        {
            if (!m_NotificationService.VerifyUrl(image, queueNumber, signiture))
            {

                return Unauthorized();
            }

            string baseImagePath = Path.Combine("wwwroot", "images", image);
            Console.WriteLine(baseImagePath);
            if (!System.IO.File.Exists(baseImagePath))
                return NotFound("Template not found");

            var bytes = QueueImageGenerator.GenerateQueueImage(baseImagePath, queueNumber);

            return File(bytes, "image/png");
        }

        public class TokenRequest
        {
            public int m_ID { get; set; } = 0;
            public string? m_Token { get; set; } = string.Empty;
        }

        public class PushRequest
        {
            public int m_UserID { get; set; } = 0;
            public string m_Title { get; set; } = string.Empty;
            public string m_Message { get; set; } = string.Empty;
            public string m_Image { get; set; } = string.Empty;
            public int m_QueueNumber { get; set; } = 0;
        }

    }
}
