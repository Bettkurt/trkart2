using TRKart.Entities.DTOs;

namespace TRKart.Business.Interfaces
{
    public interface IAuditService
    {
        Task LogAuditEventAsync(int customerId, int? sessionId, AuditEventDto auditEvent, string? ipAddress = null, string? userAgent = null);
        Task LogSecurityEventAsync(int? customerId, SecurityEventDto securityEvent, string? ipAddress = null, string? userAgent = null);
    }
}
