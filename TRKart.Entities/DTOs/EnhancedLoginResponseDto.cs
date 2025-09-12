using System;

namespace TRKart.Entities.DTOs
{
    public class EnhancedLoginResponseDto
    {
        public string Message { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset AccessTokenExpiration { get; set; }
        public DateTimeOffset RefreshTokenExpiration { get; set; }
        public UserSecurityInfoDto UserInfo { get; set; }
        public DeviceFingerprintDto DeviceInfo { get; set; }
        public string SessionID { get; set; }
        public bool RequiresAdditionalVerification { get; set; }
        public string? VerificationType { get; set; }
        public List<string> SecurityWarnings { get; set; } = new List<string>();
        public DateTimeOffset LoginTime { get; set; }
        public string? IPAddress { get; set; }
        public string? GeographicLocation { get; set; }
    }
}

