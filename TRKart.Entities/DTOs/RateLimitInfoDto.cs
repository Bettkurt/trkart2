using System;

namespace TRKart.Entities.DTOs
{
    public class RateLimitInfoDto
    {
        public string Identifier { get; set; }
        public string IdentifierType { get; set; }
        public string Endpoint { get; set; }
        public int RequestCount { get; set; }
        public DateTimeOffset FirstRequestAt { get; set; }
        public DateTimeOffset LastRequestAt { get; set; }
        public bool IsBlocked { get; set; }
        public DateTimeOffset? BlockedUntil { get; set; }
        public string? BlockReason { get; set; }
        public int RemainingAttempts { get; set; }
        public TimeSpan TimeUntilReset { get; set; }
    }
}

