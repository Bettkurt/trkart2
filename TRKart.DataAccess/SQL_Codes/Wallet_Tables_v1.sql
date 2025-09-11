-- Wallet Tables Creation Script
-- Version: 1.0
-- Description: Creates wallet related tables and updates existing transaction table

-- Create Wallets table
CREATE TABLE [dbo].[Wallets] (
    [WalletId] INT IDENTITY(1,1) NOT NULL,
    [CustomerId] INT NOT NULL,
    [Balance] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [Status] INT NOT NULL, -- Using INT to map to CardStatus enum
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Wallets] PRIMARY KEY CLUSTERED ([WalletId] ASC),
    CONSTRAINT [FK_Wallets_Customers_CustomerId] FOREIGN KEY ([CustomerId]) 
        REFERENCES [dbo].[Customers] ([CustomerId]) ON DELETE CASCADE,
    CONSTRAINT [CHK_Wallet_Balance_NotNegative] CHECK ([Balance] >= 0)
);

-- Create unique index to ensure one wallet per customer
CREATE UNIQUE NONCLUSTERED INDEX [IX_Wallets_CustomerId] 
ON [dbo].[Wallets] ([CustomerId]);

-- Add WalletId to Transaction table
ALTER TABLE [dbo].[Transaction]
ADD [WalletId] INT NULL;

-- Add foreign key constraint for WalletId
ALTER TABLE [dbo].[Transaction] WITH CHECK 
ADD CONSTRAINT [FK_Transaction_Wallets_WalletId] 
FOREIGN KEY([WalletId]) 
REFERENCES [dbo].[Wallets] ([WalletId]);

-- Add check constraints to ensure proper nullability rules
ALTER TABLE [dbo].[Transaction] WITH CHECK 
ADD CONSTRAINT [CHK_Transaction_CardOrWallet] 
CHECK (
    -- For card transactions: WalletId is NULL, and at least one card ID is NOT NULL
    ([WalletId] IS NULL AND ([SourceCardId] IS NOT NULL OR [TargetCardId] IS NOT NULL))
    OR
    -- For wallet transactions: WalletId is NOT NULL, and both card IDs are NULL
    ([WalletId] IS NOT NULL AND [SourceCardId] IS NULL AND [TargetCardId] IS NULL)
);

-- Update TransactionType enum if needed (assuming values already exist from CardStatus enum)
-- ENUM values should be: 1=LOAD, 2=PAY, 3=TRANSFER_IN, 4=TRANSFER_OUT

-- Create index on WalletId for better query performance
CREATE NONCLUSTERED INDEX [IX_Transaction_WalletId] 
ON [dbo].[Transaction] ([WalletId]);

-- Create a trigger to automatically update UpdatedAt timestamp
CREATE OR ALTER TRIGGER [dbo].[TR_Wallets_UpdateTimestamp]
ON [dbo].[Wallets]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE w
    SET UpdatedAt = GETDATE()
    FROM [dbo].[Wallets] w
    INNER JOIN inserted i ON w.WalletId = i.WalletId;
END;
GO
