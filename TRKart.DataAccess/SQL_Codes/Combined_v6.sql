-- This is the SQL code for the entire TRKart database.

-------------------------------------------------------------------------------------------
-------------------------------------Customers---------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "Customers" (
    "CustomerID" SERIAL PRIMARY KEY,
    "CustomerNumber" CHAR(10) NOT NULL UNIQUE DEFAULT 'New_Custmr',
    "FullName" VARCHAR(100),
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    -- True if the user has verified their email
    -- However, for simplicity during early development, we will not implement email verification
    -- We will set this to true as default.
    -- Later on, it will be set to false and email verification will be required
    "VerifiedUser" BOOLEAN NOT NULL DEFAULT TRUE,
    "EmailLastUpdatedAt" TIMESTAMPTZ,
    "PasswordHash" VARCHAR(200) NOT NULL,
    "PasswordChangedAt" TIMESTAMPTZ,
    "LastLoginAt" TIMESTAMPTZ,
    "FailedLoginAttempts" INTEGER DEFAULT 0,
    "AccountLockedUntil" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

-------------------------------------------------------------------------------------------
-------------------------------------PasswordHistory---------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "PasswordHistory" (
    "PasswordHistoryID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "PasswordHash" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy" VARCHAR(50) DEFAULT 'System',

    CONSTRAINT "FK_PasswordHistory_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Function to manage password history
CREATE OR REPLACE FUNCTION manage_password_history()
RETURNS TRIGGER AS $$
BEGIN
    -- Delete oldest password history if customer has 3 or more entries
    DELETE FROM "PasswordHistory"
    WHERE "PasswordHistoryID" IN (
        SELECT "PasswordHistoryID"
        FROM "PasswordHistory"
        WHERE "CustomerID" = NEW."CustomerID"
        ORDER BY "CreatedAt" ASC
        LIMIT 1
    )
    AND (
        SELECT COUNT(*)
        FROM "PasswordHistory"
        WHERE "CustomerID" = NEW."CustomerID"
    ) >= 3;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER trg_manage_password_history
BEFORE INSERT ON "PasswordHistory"
FOR EACH ROW
EXECUTE FUNCTION manage_password_history();

-------------------------------------------------------------------------------------------
-------------------------------------SessionToken------------------------------------------
-------------------------------------------------------------------------------------------

-- Create the SessionToken table
CREATE TABLE "SessionToken" (
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "AccessToken" VARCHAR(500) UNIQUE,
    "RefreshToken" VARCHAR(500) NOT NULL UNIQUE,
    "AccessTokenExpiration" TIMESTAMPTZ,
    "RefreshTokenExpiration" TIMESTAMPTZ NOT NULL,
    "RefreshTokenCreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastUsedAt" TIMESTAMPTZ,
    "UsageCount" INTEGER DEFAULT 0,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "RevokedAt" TIMESTAMPTZ,
    "RevokeReason" VARCHAR(200),
    "DeviceInfo" TEXT,
    "DeviceFingerprint" VARCHAR(255),
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "IsSuspicious" BOOLEAN DEFAULT FALSE,
    "SuspiciousReason" VARCHAR(200),
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ DEFAULT NOW(),

    CONSTRAINT "FK_SessionToken_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------
-------------------------------------TokenBlacklist----------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "TokenBlacklist" (
    "BlacklistID" SERIAL PRIMARY KEY,
    "SessionID" INTEGER NOT NULL,
    "RefreshToken" VARCHAR(500) NOT NULL,
    "BlacklistedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "BlacklistedBy" VARCHAR(50) DEFAULT 'System' CHECK ("BlacklistedBy" IN ('System', 'Customer', 'Admin')),
    "Reason" VARCHAR(200) NOT NULL,
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "SuspiciousActivity" BOOLEAN DEFAULT FALSE,
    "ComplianceRequired" BOOLEAN DEFAULT FALSE,

    CONSTRAINT "FK_TokenBlacklist_SessionToken_SessionID"
        FOREIGN KEY ("SessionID") 
        REFERENCES "SessionToken"("SessionID") 
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------
------------------------------------AuditEvents--------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "AuditEvents" (
    "AuditID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER,
    "EventType" VARCHAR(50) NOT NULL,
    "EventSubType" VARCHAR(50),
    "EventDetails" TEXT,
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "SessionID" INTEGER,
    "RiskLevel" VARCHAR(20) DEFAULT 'LOW',
    "ComplianceRequired" BOOLEAN DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_AuditEvents_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_AuditEvents_SessionToken_SessionID"
        FOREIGN KEY ("SessionID") 
        REFERENCES "SessionToken"("SessionID")
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------
-----------------------------------SecurityEvents------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "SecurityEvents" (
    "SecurityEventID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER,
    "Email" VARCHAR(100),
    "EventType" VARCHAR(50) NOT NULL,
    "EventSeverity" VARCHAR(20) NOT NULL,
    "EventDetails" TEXT,
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "DeviceFingerprint" VARCHAR(255),
    "GeographicLocation" VARCHAR(100),
    "IsResolved" BOOLEAN DEFAULT FALSE,
    "ResolvedAt" TIMESTAMPTZ,
    "ResolvedBy" VARCHAR(50),
    "ResolutionNotes" TEXT,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_SecurityEvents_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function to update LastUsedAt in SessionToken
CREATE OR REPLACE FUNCTION update_session_last_used()
RETURNS TRIGGER AS $$
BEGIN
    NEW."LastUsedAt" = NOW();
    NEW."UsageCount" = COALESCE(NEW."UsageCount", 0) + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-------------------------------------------------------------------------------------------

-- Trigger for UpdatedAt columns
CREATE TRIGGER "trg_update_customers_updated_at"
    BEFORE UPDATE ON "Customers"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER "trg_update_sessiontoken_updated_at"
    BEFORE UPDATE ON "SessionToken"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Trigger for SessionToken usage tracking
CREATE TRIGGER "trg_update_sessiontoken_usage"
    BEFORE UPDATE ON "SessionToken"
    FOR EACH ROW EXECUTE FUNCTION update_session_last_used();

-------------------------------------------------------------------------------------------
-------------------------------------RateLimiting------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "RateLimiting" (
    "RateLimitID" SERIAL PRIMARY KEY,
    "Identifier" VARCHAR(255) NOT NULL,                    -- IP address, user ID, or other identifier
    "IdentifierType" VARCHAR(20) NOT NULL,                 -- IP, USER_ID, DEVICE_ID, etc.
    "Endpoint" VARCHAR(100) NOT NULL,                      -- API endpoint being rate limited
    "CustomerID" INTEGER,                                  -- Customer ID (optional for IP-based rate limiting)
    "RequestCount" INTEGER NOT NULL DEFAULT 1,             -- Current request count in this cycle
    "FirstRequestAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),   -- When first request in this cycle was made
    "LastRequestAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),    -- When last request in this cycle was made
    "IsBlocked" BOOLEAN NOT NULL DEFAULT false,            -- Whether this identifier is currently blocked
    "BlockedUntil" TIMESTAMPTZ,                            -- When the block expires
    "BlockReason" VARCHAR(500),                            -- Reason for the block
    "ViolationCount" INTEGER DEFAULT 0,                    -- Number of times rate limit was hit
    "FirstViolationAt" TIMESTAMPTZ,                        -- When first violation occurred
    "LastViolationAt" TIMESTAMPTZ,                         -- When last violation occurred
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_RateLimiting_Customers" 
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") 
    ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Create trigger function for automatic UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_rate_limiting_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger
CREATE TRIGGER trg_rate_limiting_updated_at
    BEFORE UPDATE ON "RateLimiting"
    FOR EACH ROW
    EXECUTE FUNCTION update_rate_limiting_updated_at();

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
-------------------------------------UserCard----------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "UserCard" (
    "CardID" SERIAL PRIMARY KEY,
    "CustomerID" INT NOT NULL,
    -- CardNumber CHAR(16) because it will always be 16 characters long
    "CardNumber" CHAR(16) NOT NULL UNIQUE DEFAULT 'Generate_New_Num',
    "Balance" DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    -- 0: Deactivated, 1: Expired, 2: Lost, 3: Inactive, 4: Active
    "CardStatus" INT NOT NULL DEFAULT 3 CHECK ("CardStatus" BETWEEN 0 AND 4),
    -- 0: Standart, 1: Gold, 2: Platinum
    "CardType" INT NOT NULL DEFAULT 0 CHECK ("CardType" BETWEEN 0 AND 2),
    "CardName" VARCHAR(16),
    -- Default is calculated by DB. 5 years from current date, and end of the current month
    "CardExpirationDate" DATE NOT NULL DEFAULT 
        (DATE_TRUNC('MONTH', CURRENT_DATE) 
        + INTERVAL '5 years' 
        + INTERVAL '1 month' 
        - INTERVAL '1 day')::DATE,
    "IsBlacklisted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "LastUpdate" TIMESTAMPTZ,
    "UpdateReason" VARCHAR(100),

    CONSTRAINT "FK_UserCard_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID") 
        ON DELETE CASCADE
);

------------------------------------Number of Cards Constraint--------------------------------

-- Function to check if a customer has less than 3 cards
CREATE OR REPLACE FUNCTION check_issued_card_limit()
RETURNS TRIGGER AS $$
DECLARE
    card_count INTEGER;
BEGIN
    -- Count the number of active cards for this customer (excluding deactivated cards)
    SELECT COUNT(*) INTO card_count
    FROM "UserCard"
    WHERE "CustomerID" = NEW."CustomerID"
    -- 0: Deactivated, 1: Expired, 2: Lost, 3: Inactive, 4: Active
    -- Deactivated (As far as users concern they are deleted), and expired cards are not counted
    AND "CardStatus" > 2;

    -- If the customer already has 3 or more cards, prevent the insert
    IF card_count >= 3 THEN
        RAISE EXCEPTION 'A customer cannot have more than 3 cards';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger to enforce the card limit
CREATE OR REPLACE TRIGGER enforce_issued_card_limit_trigger
BEFORE INSERT ON "UserCard"
FOR EACH ROW
EXECUTE FUNCTION check_issued_card_limit();

-------------------------------------------------------------------------------------------
----------------------------------------CardUpdates----------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "CardUpdates" (
    "UpdateID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "PreviousStatus" INT,
    "NewStatus" INT,
    "StatusUpdatedAt" TIMESTAMPTZ,
    "PreviousType" INT,
    "NewType" INT,
    "TypeUpdatedAt" TIMESTAMPTZ,

    CONSTRAINT "FK_CardUpdates_UserCard_CardID"
    FOREIGN KEY ("CardID") 
    REFERENCES "UserCard"("CardID") 
    ON DELETE CASCADE
);

-------------------------------Log Card Status & Type Changes-------------------------------

-- Create a function to log card status changes on updates only
CREATE OR REPLACE FUNCTION log_card_update()
RETURNS TRIGGER AS $$
BEGIN
    -- Log status change
    IF OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus" THEN
        INSERT INTO "CardUpdates" ("CardID", "PreviousStatus", "NewStatus", "StatusUpdatedAt")
        VALUES (
            NEW."CardID",
            OLD."CardStatus",
            NEW."CardStatus",
            NOW()
        );
    END IF;
    
    -- Log card type change
    IF OLD."CardType" IS DISTINCT FROM NEW."CardType" THEN
        INSERT INTO "CardUpdates" ("CardID", "PreviousType", "NewType", "TypeUpdatedAt")
        VALUES (
            NEW."CardID",
            OLD."CardType",
            NEW."CardType",
            NOW()
        );
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger for status updates
CREATE OR REPLACE TRIGGER trg_card_update
AFTER UPDATE OF "CardStatus", "CardType" ON "UserCard"
FOR EACH ROW
WHEN (OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus" 
      OR OLD."CardType" IS DISTINCT FROM NEW."CardType")
EXECUTE FUNCTION log_card_update();

-------------------------------------------------------------------------------------------

-- Function to update the LastUpdate column in UserCard when CardUpdates is updated
CREATE OR REPLACE FUNCTION update_usercard_lastupdate()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the LastUpdate column in UserCard with the latest update time
    UPDATE "UserCard"
    SET 
        "LastUpdate" = COALESCE(NEW."StatusUpdatedAt", NEW."TypeUpdatedAt"),
        "UpdateReason" = CASE 
            WHEN NEW."StatusUpdatedAt" IS NOT NULL AND NEW."TypeUpdatedAt" IS NOT NULL THEN 'Status and type change'
            WHEN NEW."StatusUpdatedAt" IS NOT NULL THEN 'Status change'
            WHEN NEW."TypeUpdatedAt" IS NOT NULL THEN 'Type change'
            ELSE 'Update'
        END
    WHERE "CardID" = NEW."CardID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger to update LastUpdate after insert on CardUpdates
CREATE OR REPLACE TRIGGER trg_update_usercard_lastupdate
AFTER INSERT ON "CardUpdates"
FOR EACH ROW
EXECUTE FUNCTION update_usercard_lastupdate();

-------------------------------------------------------------------------------------------
-------------------------------------CardLimits--------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "CardLimits" (
    "LimitID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    -- Defaulted 1. Lowest tier, standart card has 20k limit. 
    -- So, if we see 1 as limit, we know something with CardType went wrong
    "PayLimit" DECIMAL(18, 2) NOT NULL DEFAULT 1.00,
    "PayMaxLimit" DECIMAL(18, 2) NOT NULL DEFAULT 1.00,
    "PayLimitUpdatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "TransferLimit" DECIMAL(18, 2) NOT NULL DEFAULT 1.00,
    "TransferMaxLimit" DECIMAL(18, 2) NOT NULL DEFAULT 1.00,
    "TransferLimitUpdatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_CardLimits_UserCard_CardID"
        FOREIGN KEY ("CardID") 
        REFERENCES "UserCard"("CardID") 
        ON DELETE CASCADE
);

-----------------------------Create Card Limits for a New Card-----------------------------

-- Function to create card limits when a new card is added
CREATE OR REPLACE FUNCTION create_card_limits_trigger_fn()
RETURNS TRIGGER AS $$
BEGIN
    -- Insert default limits based on card type
    -- Adjust the values as needed for each card type
    INSERT INTO "CardLimits" (
        "CardID",
        "PayLimit",
        "PayMaxLimit",
        "PayLimitUpdatedAt",
        "TransferLimit",
        "TransferMaxLimit",
        "TransferLimitUpdatedAt"
    ) VALUES (
        NEW."CardID",

        CASE -- PayLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END, 
        CASE -- PayMaxLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END,
        -- PayLimitUpdatedAt
        NOW(),
        
        CASE -- TransferLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END, 
        CASE -- TransferMaxLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END, 
        -- TransferLimitUpdatedAt
        NOW()  
    );
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER trg_create_card_limits_for_new_card
AFTER INSERT ON "UserCard"
FOR EACH ROW
EXECUTE FUNCTION create_card_limits_trigger_fn();

-----------------------------Handle Card Type Change for Limits-----------------------------

-- Function to handle card type changes
CREATE OR REPLACE FUNCTION handle_card_type_change_for_limits()
RETURNS TRIGGER AS $$
DECLARE
    v_old_max_pay DECIMAL(18, 2);
    v_old_max_transfer DECIMAL(18, 2);
    v_new_max_pay DECIMAL(18, 2);
    v_new_max_transfer DECIMAL(18, 2);
BEGIN
    -- Only proceed if CardType actually changed
    IF NEW."CardType" = OLD."CardType" THEN
        RETURN NEW;
    END IF;

    -- Get current max limits from CardLimits
    SELECT 
        "PayMaxLimit", 
        "TransferMaxLimit"
    INTO 
        v_old_max_pay, 
        v_old_max_transfer
    FROM "CardLimits"
    WHERE "CardID" = NEW."CardID";

    -- Calculate new max limits based on new card type
    SELECT 
        CASE 
            WHEN NEW."CardType" = 0 THEN 20000.00  -- Standard
            WHEN NEW."CardType" = 1 THEN 50000.00  -- Gold
            WHEN NEW."CardType" = 2 THEN 100000.00 -- Platinum
            ELSE v_old_max_pay
        END,
        CASE 
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE v_old_max_transfer
        END
    INTO 
        v_new_max_pay, 
        v_new_max_transfer;

    -- Determine if this is an upgrade or downgrade
    -- If new max limits are lower than old ones, it's a downgrade
    IF v_new_max_pay < v_old_max_pay OR v_new_max_transfer < v_old_max_transfer THEN
        -- Downgrade: Enforce new max limits on current limits
        UPDATE "CardLimits"
        SET 
            -- Since this is a downgare in card type/tier; 
            -- Choose the least of the current pay limit or the card's new type max pay limit
            "PayLimit" = LEAST("PayLimit", v_new_max_pay),
            "PayMaxLimit" = v_new_max_pay,
            "PayLimitUpdatedAt" = NOW(),
            -- Since this is a downgare in card type/tier; 
            -- Choose the least of the current transfer limit or the card's new type max transfer limit
            "TransferLimit" = LEAST("TransferLimit", v_new_max_transfer),
            "TransferMaxLimit" = v_new_max_transfer,
            "TransferLimitUpdatedAt" = NOW()
        WHERE "CardID" = NEW."CardID";
    ELSE
        -- Upgrade: Only update max limits, keep current limits
        UPDATE "CardLimits"
        SET 
            "PayMaxLimit" = v_new_max_pay,
            "TransferMaxLimit" = v_new_max_transfer
        WHERE "CardID" = NEW."CardID";
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE OR REPLACE TRIGGER trg_after_update_usercard_type
AFTER UPDATE OF "CardType" ON "UserCard"
FOR EACH ROW
WHEN (NEW."CardType" IS DISTINCT FROM OLD."CardType")
EXECUTE FUNCTION handle_card_type_change_for_limits();

-------------------------------------------------------------------------------------------
-------------------------------------CardBlacklist-----------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "CardBlacklist" (
    "CardBlacklistID" SERIAL PRIMARY KEY,
    "CustomerID" INT NOT NULL,       -- Direct reference to customer (FK)
    -- Original CardID for reference 
    -- Will not work as FK since cards will moved completely to this table from UserCard table
    "OriginalCardID" INT NOT NULL,  
    "CardNumber" CHAR(16) NOT NULL,  -- Copy of the card number
    -- Leftover balance 
    -- TODO: Can later be sent to another Active card of the same user
    -- Add a `JOB` to this automatically, maybe check once a week
    "LeftOverBalance" DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    "CardType" INT NOT NULL,         -- Copy of card type
    "CardExpirationDate" DATE NOT NULL,  -- Original expiration date
    "OriginalCreatedAt" TIMESTAMPTZ NOT NULL,   -- When the card was originally created
    -- 0: Deactivated, 1: Expired, 2: Reported Lost for more than 7 days
    -- TODO: Add a function/trigger to work 1 week or 2 weeks after a card is added to this list with Reason 2
    -- and change it to Reason 0 and change CardStatus to 0 (Deactivated) in UserCard table.
    -- It will also update the CardUpdates table with the new status, automatically.
    "Reason" INT NOT NULL CHECK ("Reason" BETWEEN 0 AND 2),
    -- Who blacklisted the card. CustomerID or 0 for system
    -- "BlacklistedBy" VARCHAR(50) DEFAULT 'System',
    "BlacklistedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),  -- When the card was blacklisted
    "Notes" TEXT,                    -- Any additional notes
    
    CONSTRAINT "FK_CardBlacklist_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID") 
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_CardBlacklist_UserCard_OriginalCardID"
        FOREIGN KEY ("OriginalCardID") 
        REFERENCES "UserCard"("CardID") 
        ON DELETE CASCADE
);

-------------------------------------Update UserCard---------------------------------------

-- First, create a function that will be called by the trigger
CREATE OR REPLACE FUNCTION update_user_card_blacklist_status()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the UserCard table to mark the card as blacklisted
    UPDATE "UserCard"
    SET "IsBlacklisted" = TRUE
    WHERE "CardID" = NEW."OriginalCardID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger that fires after an insert on CardBlacklist
CREATE OR REPLACE TRIGGER tr_after_card_blacklist_insert
AFTER INSERT ON "CardBlacklist"
FOR EACH ROW
EXECUTE FUNCTION update_user_card_blacklist_status();

-------------------------------------------------------------------------------------------
-------------------------------------Transaction-------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "Transaction" (
    "TransactionID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "TransferTransactionID" INT,
    "Amount" DECIMAL(18, 2) NOT NULL,
    "FeeAmount" DECIMAL(18, 2) DEFAULT 0.00,
    -- 0: Load, 1: TopUp, 2: Refund, 3: TransferIn, 4: TransferOut, 5: Pay, 6: SystemTransferIn, 7: SystemTransferOut
    "TransactionType" INT NOT NULL CHECK ("TransactionType" 
        BETWEEN 0 AND 7),
    "PaymentMethod" VARCHAR(50),
    "ExternalRef" VARCHAR(100), -- For TopUp type transactions
    "Description" TEXT,
    "Note" TEXT, -- For TopUp type transactions
    "TransactionDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "TransactionStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK ("TransactionStatus" IN ('Pending', 'Approved', 'Denied')),
    
    CONSTRAINT "FK_Transaction_UserCard_CardID"
        FOREIGN KEY ("CardID") 
        REFERENCES "UserCard"("CardID") 
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_Transaction_Transaction_TransferTransactionID"
        FOREIGN KEY ("TransferTransactionID") 
        REFERENCES "Transaction"("TransactionID") 
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Trigger function to control if a transaction can go through or not
-- It executes everytime there is a new transaction. 
CREATE OR REPLACE FUNCTION process_transaction_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_current_balance DECIMAL(18, 2);
	v_current_card_status INT;
    v_net_amount DECIMAL(18, 2);
BEGIN

    -- Check for unknown transaction statuses. 
    -- Since the trigger calls this function before inserting, and the trigger checks for TransactionStatus = 'Pending', 
    --  this should never happen. Safety check, just in case 
    IF NEW."TransactionStatus" <> 'Pending' THEN
        RAISE EXCEPTION 'Transaction is not pending. It is %', NEW."TransactionStatus";
    END IF;

    -- If the amount given is negative, something is wrong. Save it as denied for audit purposes
    IF NEW."Amount" <= 0 THEN
        NEW."TransactionStatus" := 'Denied';
        RETURN NEW;
    END IF;

    -- Get the current balance and card status with row-level locking
    SELECT "Balance", "CardStatus" 
    INTO v_current_balance, v_current_card_status
    FROM "UserCard"
    WHERE "CardID" = NEW."CardID"
    FOR UPDATE;  -- Lock the row to prevent race conditions

    -- If the card can't be found, DB won't save the entry because CardID is a FK, therefore it is required
    -- Check anyway if the card actually exists, and throw an error. Do NOT save the entry.
    IF v_current_balance IS NULL OR v_current_card_status IS NULL THEN
        RAISE EXCEPTION 'Card not found';
    END IF;
    
    -- Background services send/create transfers from blacklisted cards 
    --  to an active card of the same user (if it exists) 
    --  in order to transfer leftover balance
    -- We just approve them since all the checks are done by the back-end
    -- Back-end sends one after the other. So, it does not get mixed up

    -- First SystemTransferOut
    IF NEW."TransactionType" = 7 THEN 
        NEW."TransactionStatus" := 'Approved';
        -- Update blacklisted card balance
        UPDATE "UserCard"
        SET "Balance" = v_current_balance - NEW."Amount"
        WHERE "CardID" = NEW."CardID";

        RETURN NEW;

    -- Then, SystemTransferIn
    ELSIF NEW."TransactionType" = 6 THEN
        NEW."TransactionStatus" := 'Approved';
        -- Update active card balance
        UPDATE "UserCard"
        SET "Balance" = v_current_balance + NEW."Amount"
        WHERE "CardID" = NEW."CardID";

        RETURN NEW;
    END IF;

    -- From this point forward, we won't do any transactions for blacklisted cards
    --  They are cards with statuses 0: Deactivated, 1: Expired, 2: Lost
    -- Save it as 'Denied' for audit purposes
    IF v_current_card_status < 3 THEN
        NEW."TransactionStatus" := 'Denied';
        RETURN NEW;
    END IF;

    -- Load transactions
    IF NEW."TransactionType" = 0 THEN
        -- For load transactions, just need positive amount 
        -- Update card balance
        UPDATE "UserCard"
        SET "Balance" = v_current_balance + NEW."Amount"
        WHERE "CardID" = NEW."CardID";
                
        NEW."TransactionStatus" := 'Approved';
                
        -- Inactive cards can be activated with a 'Load'
        -- Cards are created as 'Inactive', so we need to activate them after the first load transaction
        IF v_current_card_status = 3 THEN
            UPDATE "UserCard"
            SET "CardStatus" = 4 -- Activate it (CardStatus 4: Active)
            WHERE "CardID" = NEW."CardID";
        END IF;

        RETURN NEW;
    END IF;
        
    -- From this point forward, we won't do any transactions with cards that are not active
    --  They are cards with statuses 0: Deactivated, 1: Expired, 2: Lost, 3: Inactive
    -- Save it as 'Denied' for audit purposes
    IF v_current_card_status < 4 THEN
        NEW."TransactionStatus" := 'Denied';
        RETURN NEW;
    END IF;

    -- TopUp transactions (external payment with fees)
    IF NEW."TransactionType" = 1 THEN
        -- Check for invalid fee amount values. Save it as 'Denied' for audit purposes
        IF NEW."FeeAmount" < 0 OR NEW."Amount" < NEW."FeeAmount" THEN
            NEW."TransactionStatus" := 'Denied';
            RETURN NEW;
        END IF;

        -- Calculate net amount (gross amount minus fee)
        v_net_amount := NEW."Amount" - COALESCE(NEW."FeeAmount", 0);
            
        -- Net amount must be positive. 
        -- This is a redundant check, but just in case
        IF v_net_amount < 0 THEN
            NEW."TransactionStatus" := 'Denied';
            RETURN NEW;
        END IF;

        -- Update card balance with net amount
        UPDATE "UserCard"
        SET "Balance" = "Balance" + v_net_amount
        WHERE "CardID" = NEW."CardID";

        NEW."TransactionStatus" := 'Approved';

        RETURN NEW;

    -- Pay & TransferOut transactions
    ELSIF NEW."TransactionType" = 5 OR NEW."TransactionType" = 4 THEN
        -- Check if balance is sufficient. If not, save it as 'Denied' for audit purposes
        -- These should not reach DB but just in case
        IF v_current_balance < NEW."Amount" THEN
            NEW."TransactionStatus" := 'Denied';
            RETURN NEW;
        END IF;

        -- Update card balance
        UPDATE "UserCard"
        SET "Balance" = v_current_balance - NEW."Amount"
        WHERE "CardID" = NEW."CardID";
                
        NEW."TransactionStatus" := 'Approved';
        
        RETURN NEW;

    -- Refund & TransferIn transaction
    ELSIF NEW."TransactionType" = 2 OR NEW."TransactionType" = 3 THEN
        -- Update card balance
        UPDATE "UserCard"
        SET "Balance" = "Balance" + NEW."Amount"
        WHERE "CardID" = NEW."CardID";

        NEW."TransactionStatus" := 'Approved';

        RETURN NEW;
    END IF;

    -- If the transaction is not approved or denied, something is wrong
    -- This should never happen. Safety check, just in case
    IF NEW."TransactionStatus" != 'Approved' AND NEW."TransactionStatus" != 'Denied' THEN
        RAISE EXCEPTION 'Transaction is not approved or denied. It is %', NEW."TransactionStatus";
    END IF;

END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER process_transaction_trigger
-- Before insert, so we can reject some edge cases without recording them to DB
BEFORE INSERT ON "Transaction"
FOR EACH ROW
-- New entries for Transaction table are always created with default status 'Pending'
-- So, we make sure we are just processing brand-new transactions
-- Since we check for TransactionStatus = 'Pending' in the trigger, we don't need to check it here
-- This way, we can check for entries that are not created properly in the Transaction table
-- WHEN (NEW."TransactionStatus" = 'Pending')
EXECUTE FUNCTION process_transaction_trigger();

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
