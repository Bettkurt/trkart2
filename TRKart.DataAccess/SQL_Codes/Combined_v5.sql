-- This is the SQL code for the TRKart database.

-------------------------------------------------------------------------------------------
----------------------------------Hangfire Setup-------------------------------------------
-------------------------------------------------------------------------------------------

-- Create a new role for Hangfire
CREATE ROLE hangfire_user WITH 
    LOGIN 
    PASSWORD 'HangfirePass123' 
    NOSUPERUSER 
    NOCREATEDB 
    NOCREATEROLE 
    INHERIT 
    NOREPLICATION 
    CONNECTION LIMIT -1;

-- Create the database
-- Note: This needs to be run in the postgres database first
-- CREATE DATABASE hangfiredb 
--     OWNER hangfire_user 
--     ENCODING 'UTF8' 
--     LC_COLLATE = 'en_US.UTF-8' 
--     LC_CTYPE = 'en_US.UTF-8' 
--     TEMPLATE template0;

-- Grant necessary privileges
-- Note: Run this after connecting to hangfiredb
-- GRANT ALL PRIVILEGES ON DATABASE hangfiredb TO hangfire_user;
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO hangfire_user;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO hangfire_user;
-- ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON TABLES TO hangfire_user;
-- ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON SEQUENCES TO hangfire_user;

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
    "EmailLastUpdatedAt" TIMESTAMP,
    "PasswordHash" VARCHAR(200) NOT NULL,
    "PasswordChangedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-------------------------------------------------------------------------------------------
-------------------------------------PasswordHistory---------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "PasswordHistory" (
    "PasswordHistoryID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "PasswordHash" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

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

CREATE OR REPLACE FUNCTION update_customer_password_changed_at()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE "Customers"
    SET "PasswordChangedAt" = NEW."CreatedAt"
    WHERE "CustomerID" = NEW."CustomerID";

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE OR REPLACE TRIGGER trg_update_customer_password_changed_at
AFTER INSERT ON "PasswordHistory"
FOR EACH ROW
EXECUTE FUNCTION update_customer_password_changed_at();

-------------------------------------------------------------------------------------------
-------------------------------------SessionToken------------------------------------------
-------------------------------------------------------------------------------------------

-- Create the SessionToken table
CREATE TABLE "SessionToken"
(
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "AccessToken" VARCHAR(500) UNIQUE,
    "RefreshToken" VARCHAR(500) NOT NULL UNIQUE,
    "AccessTokenExpiration" TIMESTAMP,
    "RefreshTokenExpiration" TIMESTAMP NOT NULL,
    "RefreshTokenCreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeviceInfo" TEXT,
    "IPAddress" TEXT,
    
    -- Foreign key constraint
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
    "BlacklistedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Reason" TEXT,
    "IPAddress" TEXT,

    CONSTRAINT "FK_TokenBlacklist_SessionToken_SessionID"
        FOREIGN KEY ("SessionID") 
        REFERENCES "SessionToken"("SessionID") 
        ON DELETE CASCADE
);

---------------------Update IsRevoked in SessionToken When Blacklisted---------------------

CREATE OR REPLACE FUNCTION update_sessiontoken_isrevoked()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the IsRevoked flag in the SessionToken table
    UPDATE "SessionToken"
    SET "IsRevoked" = TRUE
    WHERE "SessionID" = NEW."SessionID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE OR REPLACE TRIGGER trg_update_sessiontoken_isrevoked
AFTER INSERT ON "TokenBlacklist"
FOR EACH ROW
EXECUTE FUNCTION update_sessiontoken_isrevoked();

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
    "CardName" VARCHAR(20),
    -- Default is calculated by DB. 5 years from current date, and end of the current month
    "CardExpirationDate" DATE NOT NULL DEFAULT 
        (DATE_TRUNC('MONTH', CURRENT_DATE) 
        + INTERVAL '5 years' 
        + INTERVAL '1 month' 
        - INTERVAL '1 day')::DATE,
    "IsBlacklisted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "LastUpdate" TIMESTAMP,
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
    "StatusUpdatedAt" TIMESTAMP,
    "PreviousType" INT,
    "NewType" INT,
    "TypeUpdatedAt" TIMESTAMP,

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
            CURRENT_TIMESTAMP
        );
    END IF;
    
    -- Log card type change
    IF OLD."CardType" IS DISTINCT FROM NEW."CardType" THEN
        INSERT INTO "CardUpdates" ("CardID", "PreviousType", "NewType", "TypeUpdatedAt")
        VALUES (
            NEW."CardID",
            OLD."CardType",
            NEW."CardType",
            CURRENT_TIMESTAMP
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
    -- Defaulted 10k. Lowest tier, standart card has 20k limit. 
    -- So, if we see 10k limit, we know something with CardType went wrong
    "PayLimit" DECIMAL(18, 2) NOT NULL DEFAULT 10000.00,
    "PayMaxLimit" DECIMAL(18, 2) NOT NULL DEFAULT 10000.00,
    "PayLimitUpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "TransferLimit" DECIMAL(18, 2) NOT NULL DEFAULT 10000.00,
    "TransferMaxLimit" DECIMAL(18, 2) NOT NULL DEFAULT 10000.00,
    "TransferLimitUpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

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
        CURRENT_TIMESTAMP,
        
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
        CURRENT_TIMESTAMP  
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
            "PayLimitUpdatedAt" = CURRENT_TIMESTAMP,
            -- Since this is a downgare in card type/tier; 
            -- Choose the least of the current transfer limit or the card's new type max transfer limit
            "TransferLimit" = LEAST("TransferLimit", v_new_max_transfer),
            "TransferMaxLimit" = v_new_max_transfer,
            "TransferLimitUpdatedAt" = CURRENT_TIMESTAMP
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
    "OriginalCreatedAt" TIMESTAMP NOT NULL,   -- When the card was originally created
    -- 0: Deactivated, 1: Expired, 2: Reported Lost for more than 7 days
    -- TODO: Add a function/trigger to work 1 week after a card is added to this list with Reason 2
    -- and change it to Reason 0 and change CardStatus to 0 (Deactivated) in UserCard table.
    -- It will also update the CardUpdates table with the new status, automatically.
    "Reason" INT NOT NULL CHECK ("Reason" BETWEEN 0 AND 2),
    -- Who blacklisted the card. CustomerID or 0 for system
    -- "BlacklistedBy" INTEGER,
    "BlacklistedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- When the card was blacklisted
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
    "FeeAmount" DECIMAL(18,2) NULL DEFAULT 0.00,
    -- 0: Load, 1: TopUp, 2: Refund, 3: TransferIn, 4: TransferOut, 5: Pay, 6: SystemTransferIn, 7: SystemTransferOut
    "TransactionType" INT NOT NULL CHECK ("TransactionType" 
        BETWEEN 0 AND 7),
    "PaymentMethod" VARCHAR(50),
    "ExternalRef" VARCHAR(100), -- For TopUp type transactions
    "Description" TEXT,
    "Note" TEXT, -- For TopUp type transactions
    "TransactionDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
WHEN (NEW."TransactionStatus" = 'Pending')
EXECUTE FUNCTION process_transaction_trigger();

-------------------------------------------------------------------------------------------
---------------------------------------Indexes---------------------------------------------
-------------------------------------------------------------------------------------------

-- Indexes for faster queries
CREATE INDEX IDX_Customer_CustomerNumber ON "Customers"("CustomerNumber");
CREATE INDEX IDX_Customer_Email ON "Customers"("Email");
CREATE INDEX IDX_Customer_FullName ON "Customers"("FullName");
CREATE INDEX IDX_Customer_UpdatedAt ON "Customers"("EmailLastUpdatedAt", "PasswordChangedAt");

CREATE INDEX IDX_PasswordHistory_CustomerID ON "PasswordHistory"("CustomerID");
CREATE INDEX IDX_PasswordHistory_CreatedAt ON "PasswordHistory"("CreatedAt");

CREATE INDEX IDX_SessionToken_CustomerID ON "SessionToken" ("CustomerID");
CREATE INDEX IDX_SessionToken_RefreshToken ON "SessionToken" ("RefreshToken");
CREATE INDEX IDX_SessionToken_AccessToken ON "SessionToken" ("AccessToken");
CREATE INDEX IDX_SessionToken_Expirations ON "SessionToken" ("AccessTokenExpiration", "RefreshTokenExpiration");
CREATE INDEX IDX_SessionToken_IPAddress ON "SessionToken" ("IPAddress");

CREATE INDEX IDX_TokenBlacklist_SessionID ON "TokenBlacklist" ("SessionID");
CREATE INDEX IDX_TokenBlacklist_RefreshToken ON "TokenBlacklist"("RefreshToken");
CREATE INDEX IDX_TokenBlacklist_BlacklistedAt ON "TokenBlacklist"("BlacklistedAt");
CREATE INDEX IDX_TokenBlacklist_IPAddress ON "TokenBlacklist"("IPAddress");

CREATE INDEX IDX_UserCard_CustomerID ON "UserCard"("CustomerID");
CREATE INDEX IDX_UserCard_CardNumber ON "UserCard"("CardNumber");
CREATE INDEX IDX_UserCard_Balance ON "UserCard"("Balance");
CREATE INDEX IDX_UserCard_CreatedAt ON "UserCard"("CreatedAt");
CREATE INDEX IDX_UserCard_ExpirationDate ON "UserCard"("CardExpirationDate");
CREATE INDEX IDX_UserCard_LastUpdate ON "UserCard"("LastUpdate");

CREATE INDEX IDX_CardUpdates_CardID ON "CardUpdates"("CardID");
CREATE INDEX IDX_CardUpdates_UpdatedAt ON "CardUpdates"("StatusUpdatedAt", "TypeUpdatedAt");

CREATE INDEX IDX_CardBlacklist_CustomerID ON "CardBlacklist" ("CustomerID");
CREATE INDEX IDX_CardBlacklist_OriginalCardID ON "CardBlacklist" ("OriginalCardID");
CREATE INDEX IDX_CardBlacklist_CardNumber ON "CardBlacklist" ("CardNumber");
CREATE INDEX IDX_CardBlacklist_LeftOverBalance ON "CardBlacklist" ("LeftOverBalance");
CREATE INDEX IDX_CardBlacklist_BlacklistedAt ON "CardBlacklist" ("BlacklistedAt");

CREATE INDEX IDX_CardLimits_CardID ON "CardLimits" ("CardID");
CREATE INDEX IDX_CardLimits_Limit ON "CardLimits" ("PayLimit", "TransferLimit");
CREATE INDEX IDX_CardLimits_LimitUpdateddAt ON "CardLimits" ("PayLimitUpdatedAt", "TransferLimitUpdatedAt");

CREATE INDEX IDX_Transaction_CardID ON "Transaction"("CardID");
CREATE INDEX IDX_Transaction_TransferTransactionID ON "Transaction"("TransferTransactionID");
CREATE INDEX IDX_Transaction_Amount ON "Transaction"("Amount");
CREATE INDEX IDX_Transaction_TransactionDate ON "Transaction"("TransactionDate");

