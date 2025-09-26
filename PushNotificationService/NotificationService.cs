using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth.OAuth2;
using static Google.Apis.Requests.BatchRequest;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace PushNotificationService
{
    public class NotificationResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Response { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string? Token { get; set; }
        public bool InvalidToken { get; set; }
    }
    public class NotificationService
    {
        public ConcurrentDictionary<int, HashSet<string>> m_Tokens = new();
        private readonly string m_ProjectId = "pingpong-message";
        private readonly GoogleCredential m_Credential;
        private string? m_BaseUrl;
        private string m_SecretKey;

        public NotificationService()
        {
            Environment.GetEnvironmentVariable("FCM_SERVER_KEY");
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT"))))
            {
                m_Credential = GoogleCredential.FromStream(stream)
                                               .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            }
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            m_SecretKey = Convert.ToBase64String(key);
            m_BaseUrl = Environment.GetEnvironmentVariable("PUSH_NOTIFICATION_BASE_URL");
        }
        public async Task SendAll(string _title, string _msg, string _image, int _queueNumber)
        {
            foreach (var token in m_Tokens)
            {
                await SendUser(token.Key, _title, _msg, _image, _queueNumber);
            }
        }
        public void RegisterToken(int _id, string? _token)
        {
            if (_token == null)
            {
                Console.WriteLine($"User {_id}: Token is NULL");
                return;
            }

            if(m_Tokens.ContainsKey(_id))
            {
                m_Tokens[_id].Add(_token);
            }
            else
            {
                HashSet<string> hash = new();
                hash.Add(_token);
                m_Tokens.TryAdd(_id, hash);
            }

            Console.WriteLine($"User {_id} registered: {_token}");
        }

        public void DeregisterToken(int _id, string _token)
        {
            if (m_Tokens.ContainsKey(_id))
            {
                m_Tokens[_id].Remove(_token);
                Console.WriteLine($"User {_id} - {_token} removed");
            }
        }

        public async Task SendUser(int _id, string _title, string _msg, string _image, int _queueNumber)
        {
            if (!m_Tokens.ContainsKey(_id))
            {
                Console.WriteLine($"UserID: {_id} \nError: Has no registered token");
                return;
            }

            var url = $"{m_BaseUrl}/api/PushNotification";

            string dataToSign = $"{_queueNumber}|{_image}";
            string signature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(m_SecretKey)))
            {
                signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));
                signature = WebUtility.UrlEncode(signature);
            }

            string image = $"{url}/{_queueNumber.ToString()}?image={_image}&signiture={signature}";
            var accessToken = await m_Credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var tokens = m_Tokens[_id];

            foreach(var token in tokens)
            {
                NotificationResult result;

                if (token.Contains("ExponentPushToken"))
                {
                    Console.WriteLine("Sending to Mobile");
                    result = await SendMobileMessage(token, _title, _msg, image);
                }
                else
                    result = await SendMessage(token, client, _title, _msg, image);

                if (result.Success == false)
                {
                    if (result.InvalidToken)
                        DeregisterToken(_id, token);
                    Console.WriteLine($"UserID: {_id}\nToken:{token} \nError:{result.Error} \nResponse:{result.Response}");
                }
            }
        }

        public bool VerifyUrl(string _image, int _queueNumber, string _signiture)
        {
            string dataToSign = $"{_queueNumber}|{_image}";
            string expectedSigniture;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(m_SecretKey)))
            {
                expectedSigniture = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));
            }
            var decodedSignature = WebUtility.UrlDecode(_signiture);
            return expectedSigniture == decodedSignature;
        }

        public async Task<NotificationResult> SendMobileMessage(string _token, string _title, string _msg, string _image)
        {
            using var client = new HttpClient();
            var payload = new
            {
                to = _token,
                sound = "default",
                title = _title,
                body = _msg,
                image = _image,
                data = new { image = _image }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("https://exp.host/--/api/v2/push/send", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new NotificationResult
                    {
                        Success = true,
                        Token = _token,
                        StatusCode = (int)response.StatusCode,
                        Response = responseString,
                        InvalidToken = false
                    };
                }

                return new NotificationResult
                {
                    Success = false,
                    Error = "Token invalid/expired",
                    Token = _token,
                    StatusCode = (int)response.StatusCode,
                    Response = responseString,
                    InvalidToken = true
                };
            }
            catch (Exception ex)
            {
                return new NotificationResult
                {
                    Success = false,
                    Error = ex.Message,
                    Token = _token,
                    InvalidToken = false
                };
            }
        }
        public async Task<NotificationResult> SendMessage(string _token, HttpClient _client, string _title, string _msg, string _image)
        {
            try
            {
                var payload = new
                {
                    message = new
                    {
                        token = _token,
                        data = new
                        {
                            title = _title,
                            body = _msg,
                            image = _image
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(
                    $"https://fcm.googleapis.com/v1/projects/{m_ProjectId}/messages:send",
                    content);

                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(result);
                    var error = doc.RootElement.GetProperty("error");
                    string status = error.GetProperty("status").GetString();

                    if (status == "INVALID_ARGUMENT" || status == "NOT_FOUND")
                    {
                        return new NotificationResult
                        {
                            Success = false,
                            Error = "Token invalid/expired",
                            Token = _token,
                            StatusCode = (int)response.StatusCode,
                            Response = result,
                            InvalidToken = true
                        };
                    }
                }

                return new NotificationResult
                {
                    Success = true,
                    Token = _token,
                    StatusCode = (int)response.StatusCode,
                    Response = result,
                    InvalidToken = false
                };
            }
            catch (Exception ex)
            {
                return new NotificationResult
                {
                    Success = false,
                    Error = ex.Message,
                    Token = _token,
                    InvalidToken = false
                };
            }
        }
    }
}
