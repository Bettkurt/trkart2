# TRKart Backend Logging Implementation

## Overview
This document describes the comprehensive logging implementation added to the TRKart backend API to improve debugging, monitoring, and troubleshooting capabilities.

## What Was Implemented

### 1. Serilog Configuration
- **File**: `Program.cs`
- **Features**:
  - Console logging for development
  - File logging with daily rotation
  - 30-day log retention
  - 10MB file size limit per log file
  - Configuration-based setup

### 2. Logging Configuration
- **File**: `appsettings.json`
- **Features**:
  - Structured logging levels
  - Namespace-specific log levels
  - Serilog configuration
  - Console and file output

### 3. Controllers with Logging
- **AuthController**: Comprehensive authentication logging
- **TransactionController**: Transaction processing logging
- **Features**:
  - Request/response logging
  - Error logging with context
  - Security event logging
  - Performance tracking

### 4. Business Services with Logging
- **AuthService**: Authentication business logic logging
- **UserCardService**: Card management logging
- **Features**:
  - Business operation logging
  - Database operation tracking
  - Error handling with context
  - Success/failure logging

## Log Levels Used

### Information Level
- User actions (login, logout, registration)
- Successful operations
- Important business events

### Warning Level
- Validation failures
- Security concerns
- Business rule violations

### Error Level
- Exceptions and errors
- Database failures
- System errors

### Debug Level
- Detailed operation tracking
- Development information
- Performance metrics

## Log File Structure

```
logs/
└── trkart-api-YYYYMMDD.txt
```

### Log Format
- Timestamp
- Log level
- Source (namespace/class)
- Message
- Structured data (parameters)
- Exception details (when applicable)

## Security Considerations

### Sensitive Data Protection
- **Tokens**: Only first 10 characters logged
- **Passwords**: Never logged
- **Personal Data**: Minimal logging for privacy

### Audit Trail
- IP address logging for security events
- User action tracking
- Session management logging

## Performance Impact

### Minimal Overhead
- Async logging operations
- Structured logging for efficient parsing
- Configurable log levels per environment

### File Management
- Daily log rotation
- Automatic cleanup of old logs
- Size-based file limits

## Usage Examples

### Controller Logging
```csharp
_logger.LogInformation("Login attempt for email: {Email} from IP: {IPAddress}", dto.Email, ipAddress);
_logger.LogWarning("Login failed - invalid credentials for email: {Email}", dto.Email);
_logger.LogError(ex, "Login error for email: {Email}", dto.Email);
```

### Service Logging
```csharp
_logger.LogDebug("Starting database operation for customer: {CustomerID}", customerId);
_logger.LogInformation("Operation completed successfully for customer: {CustomerID}", customerId);
_logger.LogError(ex, "Database operation failed for customer: {CustomerID}", customerId);
```

## Environment-Specific Configuration

### Development
- Console logging enabled
- Debug level logging
- Detailed error information

### Production
- File logging only
- Information level and above
- Minimal sensitive data exposure

## Monitoring and Maintenance

### Log Analysis
- Structured format for easy parsing
- Consistent message patterns
- Correlation IDs for request tracking

### Maintenance Tasks
- Daily log rotation
- 30-day retention policy
- 10MB file size limits

## Benefits

1. **Debugging**: Quick identification of issues
2. **Monitoring**: Real-time system health tracking
3. **Security**: Audit trail for security events
4. **Performance**: Identify bottlenecks and slow operations
5. **Compliance**: Meet logging requirements for financial applications

## Next Steps

1. **Add logging to remaining controllers** (UserCardController, TransferController, etc.)
2. **Implement log aggregation** for production environments
3. **Add performance metrics logging**
4. **Create log analysis dashboards**
5. **Implement alerting for critical errors**

## Files Modified

- `Program.cs` - Serilog configuration
- `appsettings.json` - Logging configuration
- `Controllers/AuthController.cs` - Authentication logging
- `Controllers/TransactionController.cs` - Transaction logging
- `Services/AuthService.cs` - Authentication service logging
- `Services/UserCardService.cs` - Card service logging

## Dependencies Added

- `Serilog.AspNetCore` - Main logging framework
- `Serilog.Settings.Configuration` - Configuration support
- `Serilog.Sinks.File` - File logging support
- `Serilog.Sinks.Console` - Console logging support
