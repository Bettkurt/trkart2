using System;
using System.Collections.Generic;

namespace TRKart.Entities.DTOs
{
    public class ComplianceReportDto
    {
        public DateTimeOffset ReportDate { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveSessions { get; set; }
        public int BlacklistedTokens { get; set; }
        public int SecurityEvents { get; set; }
        public int ComplianceRequiredEvents { get; set; }
        public List<ComplianceEventDto> RecentComplianceEvents { get; set; } = new List<ComplianceEventDto>();
        public string OverallComplianceStatus { get; set; } = "COMPLIANT";
        public List<string> ComplianceWarnings { get; set; } = new List<string>();
        public List<string> ComplianceRecommendations { get; set; } = new List<string>();
    }

    public class ComplianceEventDto
    {
        public int EventID { get; set; }
        public string EventType { get; set; }
        public string EventSubType { get; set; }
        public string? EventDetails { get; set; }
        public string RiskLevel { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public int? CustomerID { get; set; }
        public string? CustomerEmail { get; set; }
        public bool RequiresAction { get; set; }
        public string? ActionRequired { get; set; }
    }
}

