using System;
using System.Collections.Generic;

namespace TRKart.Entities.DTOs
{
    public class UserSecurityInfoDto
    {
        public int CustomerID { get; set; }
        public string Email { get; set; }
        public string? FullName { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTimeOffset? AccountLockedUntil { get; set; }
        public bool IsAccountLocked { get; set; }
        public int ActiveSessionCount { get; set; }
        public List<SessionInfoDto> ActiveSessions { get; set; } = new List<SessionInfoDto>();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}

