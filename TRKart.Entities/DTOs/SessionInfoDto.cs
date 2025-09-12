using System;

namespace TRKart.Entities.DTOs
{
    public class SessionInfoDto
    {
        public int SessionID { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTimeOffset? LastUsedAt { get; set; }
        public int UsageCount { get; set; }
        public bool IsSuspicious { get; set; }
        public string? SuspiciousReason { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset AccessTokenExpiration { get; set; }
        public DateTimeOffset RefreshTokenExpiration { get; set; }
    }
}

