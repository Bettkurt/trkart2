using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TRKart.Business.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;

namespace TRKart.Business.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAuditEventAsync(int customerId, int? sessionId, AuditEventDto auditEvent, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var auditEventEntity = new AuditEvents
                {
                    CustomerID = customerId,
                    SessionID = sessionId,
                    EventType = auditEvent.EventType,
                    EventSubType = auditEvent.EventSubType,
                    EventDetails = auditEvent.EventDetails,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    RiskLevel = auditEvent.RiskLevel ?? "LOW",
                    ComplianceRequired = auditEvent.ComplianceRequired
                };

                _context.AuditEvents.Add(auditEventEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit event logged: {EventType} for customer {CustomerId}", 
                    auditEvent.EventType, customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit event for customer {CustomerId}", customerId);
            }
        }

        public async Task LogSecurityEventAsync(int? customerId, SecurityEventDto securityEvent, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var securityEventEntity = new SecurityEvents
                {
                    CustomerID = customerId,
                    Email = securityEvent.Email,
                    EventType = securityEvent.EventType,
                    EventSeverity = securityEvent.EventSeverity,
                    EventDetails = securityEvent.EventDetails,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    DeviceFingerprint = securityEvent.DeviceFingerprint,
                    GeographicLocation = securityEvent.GeographicLocation
                };

                _context.SecurityEvents.Add(securityEventEntity);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Security event logged: {EventType} ({Severity}) for customer {CustomerId}", 
                    securityEvent.EventType, securityEvent.EventSeverity, customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event for customer {CustomerId}", customerId);
            }
        }
    }
}
