-------------------------------------------------------------------------------------------
-------------------------------------Views-------------------------------------------------
-------------------------------------------------------------------------------------------

-- Active sessions view
CREATE VIEW "ActiveSessions" AS
SELECT 
    s.*,
    c."Email",
    c."FullName"
FROM "SessionToken" s
JOIN "Customers" c ON s."CustomerID" = c."CustomerID"
WHERE s."IsRevoked" = FALSE 
  AND s."RefreshTokenExpiration" > NOW()
  AND s."AccessTokenExpiration" > NOW();

-- Suspicious sessions view
CREATE VIEW "SuspiciousSessions" AS
SELECT 
    s.*,
    c."Email",
    c."FullName"
FROM "SessionToken" s
JOIN "Customers" c ON s."CustomerID" = c."CustomerID"
WHERE s."IsSuspicious" = TRUE 
  AND s."IsRevoked" = FALSE;

-- Compliance audit view
CREATE VIEW "ComplianceAudit" AS
SELECT 
    a.*,
    c."Email",
    c."FullName"
FROM "AuditEvents" a
LEFT JOIN "Customers" c ON a."CustomerID" = c."CustomerID"
WHERE a."ComplianceRequired" = TRUE
ORDER BY a."CreatedAt" DESC;

-------------------------------------------------------------------------------------------
---------------------------------------Indexes---------------------------------------------
-------------------------------------------------------------------------------------------

-- Customers indexes
CREATE INDEX  "IDX_Customer_CustomerNumber" ON "Customers"("CustomerNumber");
CREATE INDEX  "IDX_Customer_Email" ON "Customers"("Email");
CREATE INDEX  "IDX_Customer_FullName" ON "Customers"("FullName");
CREATE INDEX  "IDX_Customer_UpdatedAt" ON "Customers"("EmailLastUpdatedAt", "PasswordChangedAt");

-- PasswordHistory indexes
CREATE INDEX  "IDX_PasswordHistory_CustomerID" ON "PasswordHistory"("CustomerID");
CREATE INDEX  "IDX_PasswordHistory_CreatedAt" ON "PasswordHistory"("CreatedAt");

-- SessionToken indexes
CREATE INDEX  "IDX_SessionToken_CustomerID" ON "SessionToken" ("CustomerID");
CREATE INDEX  "IDX_SessionToken_RefreshToken" ON "SessionToken" ("RefreshToken");
CREATE INDEX  "IDX_SessionToken_AccessToken" ON "SessionToken" ("AccessToken");
CREATE INDEX  "IDX_SessionToken_Validation" ON "SessionToken" ("RefreshToken", "RefreshTokenExpiration", "IsRevoked");
CREATE INDEX  "IDX_SessionToken_AccessValidation" ON "SessionToken" ("AccessToken", "AccessTokenExpiration", "IsRevoked");
CREATE INDEX  "IDX_SessionToken_CustomerActive" ON "SessionToken" ("CustomerID", "IsRevoked", "RefreshTokenExpiration");
CREATE INDEX  "IDX_SessionToken_LastUsed" ON "SessionToken" ("LastUsedAt");
CREATE INDEX  "IDX_SessionToken_Suspicious" ON "SessionToken" ("IsSuspicious", "CreatedAt");

-- TokenBlacklist indexes
CREATE INDEX  "IDX_TokenBlacklist_RefreshToken" ON "TokenBlacklist" ("RefreshToken");
CREATE INDEX  "IDX_TokenBlacklist_SessionID" ON "TokenBlacklist" ("SessionID");
CREATE INDEX  "IDX_TokenBlacklist_BlacklistedAt" ON "TokenBlacklist" ("BlacklistedAt");
CREATE INDEX  "IDX_TokenBlacklist_Suspicious" ON "TokenBlacklist" ("SuspiciousActivity", "BlacklistedAt");

-- Audit and Security indexes
CREATE INDEX  "IDX_AuditEvents_CustomerID" ON "AuditEvents" ("CustomerID", "CreatedAt");
CREATE INDEX  "IDX_AuditEvents_EventType" ON "AuditEvents" ("EventType", "CreatedAt");
CREATE INDEX  "IDX_AuditEvents_Compliance" ON "AuditEvents" ("ComplianceRequired", "CreatedAt");

CREATE INDEX  "IDX_SecurityEvents_CustomerID" ON "SecurityEvents" ("CustomerID", "CreatedAt");
CREATE INDEX  "IDX_SecurityEvents_Severity" ON "SecurityEvents" ("EventSeverity", "CreatedAt");
CREATE INDEX  "IDX_SecurityEvents_Unresolved" ON "SecurityEvents" ("IsResolved", "CreatedAt");

-- Rate limiting indexes
CREATE INDEX  "IDX_RateLimiting_Identifier" ON "RateLimiting" ("Identifier", "IdentifierType");
CREATE INDEX  "IDX_RateLimiting_Blocked" ON "RateLimiting" ("IsBlocked", "BlockedUntil");

-- UserCard indexes
CREATE INDEX  "IDX_UserCard_CustomerID" ON "UserCard"("CustomerID");
CREATE INDEX  "IDX_UserCard_CardNumber" ON "UserCard"("CardNumber");
CREATE INDEX  "IDX_UserCard_Balance" ON "UserCard"("Balance");
CREATE INDEX  "IDX_UserCard_CreatedAt" ON "UserCard"("CreatedAt");
CREATE INDEX  "IDX_UserCard_ExpirationDate" ON "UserCard"("CardExpirationDate");
CREATE INDEX  "IDX_UserCard_LastUpdate" ON "UserCard"("LastUpdate");

-- CardUpdates indexes
CREATE INDEX  "IDX_CardUpdates_CardID" ON "CardUpdates"("CardID");
CREATE INDEX  "IDX_CardUpdates_UpdatedAt" ON "CardUpdates"("StatusUpdatedAt", "TypeUpdatedAt");

-- CardBlacklist indexes
CREATE INDEX  "IDX_CardBlacklist_CustomerID" ON "CardBlacklist" ("CustomerID");
CREATE INDEX  "IDX_CardBlacklist_OriginalCardID" ON "CardBlacklist" ("OriginalCardID");
CREATE INDEX  "IDX_CardBlacklist_CardNumber" ON "CardBlacklist" ("CardNumber");
CREATE INDEX  "IDX_CardBlacklist_LeftOverBalance" ON "CardBlacklist" ("LeftOverBalance");
CREATE INDEX  "IDX_CardBlacklist_BlacklistedAt" ON "CardBlacklist" ("BlacklistedAt");

-- CardLimits indexes
CREATE INDEX  "IDX_CardLimits_CardID" ON "CardLimits" ("CardID");
CREATE INDEX  "IDX_CardLimits_Limit" ON "CardLimits" ("PayLimit", "TransferLimit");
CREATE INDEX  "IDX_CardLimits_LimitUpdateddAt" ON "CardLimits" ("PayLimitUpdatedAt", "TransferLimitUpdatedAt");

-- Transaction indexes
CREATE INDEX  "IDX_Transaction_CardID" ON "Transaction"("CardID");
CREATE INDEX  "IDX_Transaction_TransferTransactionID" ON "Transaction"("TransferTransactionID");
CREATE INDEX  "IDX_Transaction_Amount" ON "Transaction"("Amount");
CREATE INDEX  "IDX_Transaction_TransactionDate" ON "Transaction"("TransactionDate");