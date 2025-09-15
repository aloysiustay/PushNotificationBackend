using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth.OAuth2;
using static System.Net.Mime.MediaTypeNames;

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
                m_NotificationService.SendAll("Test", "Testing", "notification_1.png");

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
                m_NotificationService.SendAll(request.m_Title, request.m_Message, request.m_Image);

                return Ok(new { message = "Push Notification Successful" });
            }
            return BadRequest(new { error = "Push Notification Unsuccessful" });
        }

        public class TokenRequest
        {
            public int m_ID { get; set; } = 0;
            public string m_Token { get; set; } = string.Empty;
        }

        public class PushRequest
        {
            public string m_Title { get; set; } = string.Empty;
            public string m_Message { get; set; } = string.Empty;
            public string m_Image { get; set; } = string.Empty;
        }

    }
}
