using System;
using System.Collections.Generic;

namespace TRKart.Entities.DTOs
{
    public class SecurityAuditDto
    {
        public int CustomerID { get; set; }
        public string Email { get; set; }
        public string? FullName { get; set; }
        public List<AuditEventDto> RecentAuditEvents { get; set; } = new List<AuditEventDto>();
        public List<SecurityEventDto> RecentSecurityEvents { get; set; } = new List<SecurityEventDto>();
        public List<SessionInfoDto> SuspiciousSessions { get; set; } = new List<SessionInfoDto>();
        public int TotalAuditEvents { get; set; }
        public int TotalSecurityEvents { get; set; }
        public int SuspiciousSessionCount { get; set; }
        public DateTimeOffset LastSecurityReview { get; set; }
        public string OverallRiskLevel { get; set; } = "LOW";
    }
}

