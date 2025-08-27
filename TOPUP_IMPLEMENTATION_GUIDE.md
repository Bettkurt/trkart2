# Top-Up Feature Implementation Guide

## Overview
A comprehensive Top-Up feature has been implemented for the TRKart transaction system, allowing users to add balance to their cards from external payment sources with full security, validation, and idempotency controls.

## ✅ Completed Implementation

### Backend Features
- **Database Schema**: Updated with TopUp transaction type, ExternalRef (unique), PaymentMethod, FeeAmount, Note columns
- **Amount Precision**: Upgraded to decimal(18,2) for higher precision
- **TopUpService**: Full implementation with validation, idempotency checks, card status verification
- **API Endpoints**: Secure endpoints with authentication and authorization
- **Database Triggers**: Updated to handle TopUp transaction processing
- **ACID Transactions**: Row-level locking for concurrent safety

### Frontend Features  
- **TopUpForm Component**: Real-time validation and user-friendly interface
- **TopUpPage**: Comprehensive UI with help sections and security information
- **App Integration**: Routing, navigation, and dashboard integration
- **Transaction Lists**: TopUp transactions display distinctly with special styling
- **Responsive Design**: Mobile-friendly interface

### Security & Business Rules
- **Authentication Required**: Only authenticated users can perform top-ups
- **Authorization**: Users can only top up their own cards
- **Card Status**: Only Active cards can be topped up
- **Amount Limits**: $10 - $10,000 per transaction
- **Fee Handling**: Gross amount minus fees = net amount added to card
- **Idempotency**: ExternalRef prevents duplicate transactions
- **Logging**: Comprehensive logging with CorrelationId for tracking

## 🚀 Getting Started

### 1. Database Setup
Run the migration script to update your database:
```sql
-- Execute this file in your PostgreSQL database
-- File: TRKart.DataAccess/SQL_Codes/TopUp_Migration.sql
```

### 2. Backend Setup
The backend is already configured:
- TopUpService registered in DI container
- API endpoints available at `/api/securetransaction/topup`
- Authentication middleware configured

### 3. Frontend Setup
Start the development server:
```bash
cd TRKartFrontend
npm run dev
```

## 📍 Access Points

### User Interface
- **Main Page**: `/top-up` - Complete top-up form
- **Dashboard**: Quick Actions → "💳 Top-Up Card" button
- **Transactions**: Navigation → "💳 Top-Up" button

### API Endpoints
- `POST /api/securetransaction/topup` - Process top-up request
- `POST /api/securetransaction/topup/validate` - Validate top-up request
- `GET /api/securetransaction/topups` - Get user's top-up history
- `POST /api/securetransaction/topup/{id}/simulate-approval` - Development approval simulation

## 🔧 Configuration

### Payment Methods Supported
- Credit Card
- Wire Transfer
- Bank Transfer
- PayPal
- Stripe
- Cash (at participating locations)

### Transaction Limits
- **Minimum**: $10.00 per transaction
- **Maximum**: $10,000.00 per transaction
- **Daily Limits**: Contact support for higher limits

### Fee Structure
- Optional processing fees can be specified
- Fees are deducted from gross amount
- Net amount = Gross amount - Fee amount

## 🧪 Testing Scenarios

### Valid Test Cases
1. **Basic Top-Up**: Active card, valid amount ($10-$10,000), supported payment method
2. **With Fees**: Gross amount with fee deduction
3. **External Reference**: Using ExternalRef for payment provider tracking
4. **Idempotency**: Same ExternalRef returns existing transaction

### Invalid Test Cases
1. **Inactive Card**: Should be denied
2. **Amount Limits**: Below $10 or above $10,000 should be rejected
3. **Invalid Card**: Non-existent card number should be rejected
4. **Unauthorized**: Trying to top up another user's card should be denied
5. **Invalid Fees**: Fee >= gross amount should be rejected

### Development Features
- **Auto-Approval**: Transactions are automatically approved in development
- **Test Auth**: Test authentication button available in development mode
- **Simulation**: Manual approval simulation endpoints for testing

## 🔐 Security Features

### Input Validation
- Card number format validation (16 alphanumeric characters)
- Amount validation (positive, within limits)
- Payment method validation
- SQL injection protection
- XSS protection

### Authentication & Authorization
- JWT-based authentication
- Session validation
- Customer ID verification
- Role-based access control

### Data Protection
- Sensitive data masking in logs
- Secure HTTP communications
- CORS protection
- Request validation

### Idempotency & Concurrency
- ExternalRef uniqueness constraints
- Database row-level locking
- Optimistic concurrency tokens
- Transaction rollback on errors

## 🏗️ Architecture

### Database Tables
- **Transaction**: Updated with TopUp support
- **UserCard**: Active status validation
- **Customers**: User authentication

### Services
- **TopUpService**: Core business logic
- **InputValidationService**: Input validation
- **AuthService**: Authentication
- **TransactionService**: Base transaction functionality

### API Layer
- **SecureTransactionController**: Authenticated endpoints
- **Middleware**: JWT validation, authentication
- **Error Handling**: Comprehensive error responses

## 📊 Monitoring & Logging

### Correlation IDs
Each top-up request gets a unique correlation ID for tracking across services.

### Log Levels
- **Info**: Successful operations, key milestones
- **Warning**: Business rule violations, validation failures
- **Error**: System errors, exceptions
- **Debug**: Detailed debugging information (development only)

### Metrics to Monitor
- Top-up success/failure rates
- Average processing time
- Amount distributions
- Payment method usage
- Error patterns

## 🛠️ Troubleshooting

### Common Issues
1. **Card Not Found**: Verify card number format and existence
2. **Authentication Failed**: Check JWT tokens and session validity
3. **Amount Rejected**: Verify amount is within $10-$10,000 range
4. **Duplicate ExternalRef**: Check if transaction already exists
5. **Card Not Active**: Verify card status in database

### Debug Commands
```bash
# Check authentication status
curl -b cookies.txt http://localhost:7037/api/SecureTransaction/test-auth

# Validate top-up request
curl -X POST -H "Content-Type: application/json" \
  -b cookies.txt \
  -d '{"targetCardNumber":"CARD123456789012","amount":100,"paymentMethod":"CreditCard"}' \
  http://localhost:7037/api/SecureTransaction/topup/validate
```

## 🔄 Future Enhancements

### Webhook Integration
- Payment provider webhook handling
- Signature verification implementation
- Replay attack prevention
- Retry mechanisms

### Advanced Features
- Multi-currency support
- Recurring top-ups
- Group/family card top-ups
- Mobile app integration
- Notification system

### Analytics
- Top-up trends analysis
- Fraud detection
- User behavior insights
- Performance optimization

## 📋 Production Checklist

Before deploying to production:

- [ ] Run migration script on production database
- [ ] Configure payment provider webhooks
- [ ] Set up proper logging infrastructure
- [ ] Configure SSL certificates
- [ ] Set production environment variables
- [ ] Test all authentication flows
- [ ] Verify amount limits and business rules
- [ ] Test error handling scenarios
- [ ] Set up monitoring and alerts
- [ ] Review security configurations

## 🆘 Support

For issues or questions:
1. Check the troubleshooting section above
2. Review application logs with correlation IDs
3. Verify database constraints and triggers
4. Test authentication and authorization
5. Contact development team with specific error details

---

**Implementation Date**: August 2024  
**Version**: 1.0  
**Status**: Complete and Ready for Testing ✅
