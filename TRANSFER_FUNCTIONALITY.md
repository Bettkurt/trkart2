# TRKart Transfer Functionality

## Overview

The TRKart application now includes a dedicated transfer system that allows users to transfer money between cards securely with full transaction tracking and audit trails.

## Features

### Frontend Components

1. **TransferForm Component** (`TRKartFrontend/src/components/TransferForm.tsx`)
   - Sender card selection dropdown (shows user's cards with balances)
   - Recipient card number input field
   - Transfer amount input with validation
   - Real-time form validation
   - Loading states and error handling

2. **NewTransferPage** (`TRKartFrontend/src/pages/NewTransferPage.tsx`)
   - Dedicated page for transfer operations
   - Clean, user-friendly interface
   - Information section explaining transfer process
   - Security notices and best practices

3. **TransferService** (`TRKartFrontend/src/services/transferService.ts`)
   - API integration for transfer operations
   - Card validation endpoints
   - Transfer history and details retrieval

### Backend Components

1. **TransferCreateDto** (`TRKart.Entities/DTOs/TransferCreateDto.cs`)
   - Data transfer object for transfer requests
   - Input validation with data annotations
   - Sender card ID, recipient card number, and amount

2. **TransferService** (`TRKart.Business/Services/TransferService.cs`)
   - Business logic for transfer operations
   - Creates linked TransferIn and TransferOut transactions
   - Validates sender and recipient cards
   - Ensures sufficient balance and card status

3. **TransferController** (`TRKart.API/Controllers/TransferController.cs`)
   - REST API endpoints for transfer operations
   - `/api/Transfer/create` - Create new transfer
   - `/api/Transfer/validate-recipient/{cardNumber}` - Validate recipient card
   - `/api/Transfer/details/{transferTransactionID}` - Get transfer details

## Transfer Flow

### 1. User Interface Flow
1. User navigates to `/new-transfer`
2. Selects their card from dropdown (sender)
3. Enters recipient card number manually
4. Specifies transfer amount
5. Clicks "Send Transfer" button

### 2. Backend Processing Flow
1. **Validation**: Check sender card exists and has sufficient balance
2. **Recipient Lookup**: Find recipient card by card number
3. **Create TransferIn**: Create transaction for recipient (positive amount)
4. **Create TransferOut**: Create transaction for sender (negative amount)
5. **Link Transactions**: Cross-reference both transactions for traceability

### 3. Transaction Structure

The system creates two linked transactions:

| Type | CardID | Amount | TransactionID | transferTransactionId |
|------|--------|--------|---------------|---------------------|
| TransferOut | senderCard | -100 | tx456 | tx123 |
| TransferIn | receiverCard | +100 | tx123 | tx456 |

## Security Features

- **Input Validation**: All inputs are validated on both frontend and backend
- **Balance Checks**: Ensures sender has sufficient funds
- **Card Status Validation**: Only active cards can participate in transfers
- **Cross-Linking**: All transfers are traceable through linked transaction IDs
- **Audit Trail**: Complete transaction history maintained

## API Endpoints

### Create Transfer
```
POST /api/Transfer/create
Content-Type: application/json
Authorization: Bearer {token}

{
  "senderCardID": 123,
  "recipientCardNumber": "CARD123456",
  "amount": 100.00
}
```

### Validate Recipient Card
```
GET /api/Transfer/validate-recipient/{cardNumber}
Authorization: Bearer {token}
```

### Get Transfer Details
```
GET /api/Transfer/details/{transferTransactionID}
Authorization: Bearer {token}
```

## Database Schema

The transfer system leverages the existing Transaction table with the `TransferTransactionID` field to create cross-references between TransferIn and TransferOut transactions.

## Usage Instructions

1. **Access**: Navigate to Dashboard → "New Transfer" button
2. **Select Card**: Choose your card from the dropdown
3. **Enter Recipient**: Type the recipient's card number
4. **Specify Amount**: Enter the transfer amount
5. **Submit**: Click "Send Transfer" to complete the operation

## Error Handling

- **Insufficient Balance**: Clear error message if sender lacks funds
- **Invalid Card**: Validation for recipient card existence and status
- **Network Errors**: Graceful handling of API communication issues
- **Validation Errors**: Real-time feedback for form validation issues

## Future Enhancements

- Transfer history page
- Scheduled transfers
- Transfer limits and restrictions
- Bulk transfer operations
- Transfer notifications
- Transfer reversal functionality 