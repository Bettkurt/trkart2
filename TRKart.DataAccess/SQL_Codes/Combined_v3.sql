
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
    -- We will set this true. Later on, it will be set to false and email verification will be required
    "VerifiedUser" BOOLEAN NOT NULL DEFAULT TRUE, 
    "PasswordHash" VARCHAR(200) NOT NULL
);


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
    "IsRevoked" BOOLEAN NOT NULL DEFAULT false,
    "DeviceInfo" TEXT,
    "IPAddress" TEXT,
    
    -- Foreign key constraint
    CONSTRAINT "FK_SessionToken_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------
-------------------------------------TokenBlacklist---------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "TokenBlacklist" (
    "BlacklistID" SERIAL PRIMARY KEY,
    "RefreshToken" VARCHAR(500) NOT NULL,
    "BlacklistedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Reason" TEXT,
    "IPAddress" VARCHAR(45)
);

---------------------------------------Functions-------------------------------------------

-- Create function to purge expired tokens
CREATE OR REPLACE FUNCTION purge_expired_tokens()
RETURNS void AS $$
DECLARE
    purge_before TIMESTAMP;
BEGIN
    -- Remove sessions that expired more than 30 days ago
    purge_before := NOW() - INTERVAL '30 days';

    DELETE FROM "SessionToken"
    WHERE "RefreshTokenExpiration" < purge_before;

    -- Remove blacklist entries older than 90 days
    purge_before := NOW() - INTERVAL '90 days';

    DELETE FROM "TokenBlacklist"
    WHERE "BlacklistedAt" < purge_before;

    RAISE NOTICE 'Token cleanup completed at %', NOW();
END;
$$ LANGUAGE plpgsql;

-------------------------------------------------------------------------------------------
-------------------------------------UserCard---------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "UserCard" (
    "CardID" SERIAL PRIMARY KEY,
    -- CardNumber CHAR(16) because it will always be 16 characters long
    "CardNumber" CHAR(16) NOT NULL UNIQUE DEFAULT 'Generate_New_Num',
    "CustomerID" INT NOT NULL,
    "Balance" DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    -- 'Active' or 'Inactive'. We may add different status types in the future, 
    -- i.e. 'Lost', 'Suspended', 'Deleted' etc.; based on the feature requirements
    "CardStatus" VARCHAR(20) NOT NULL DEFAULT 'Inactive' CHECK ("CardStatus" IN ('Active', 'Inactive', 'Lost', 'Expired', 'Deactivated')),
    "CardName" VARCHAR(20),
    "CardExpirationDate" DATE NOT NULL DEFAULT (DATE_TRUNC('MONTH', CURRENT_DATE) + INTERVAL '5 years' + INTERVAL '1 month' - INTERVAL '1 day')::DATE,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "LastUpdate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") ON DELETE CASCADE
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
    AND "CardStatus" != 'Deactivated';

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
----------------------------------CardUpdates----------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "CardUpdates" (
    "UpdateID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "PreviousStatus" VARCHAR(20) NOT NULL,
    "NewStatus" VARCHAR(20) NOT NULL,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CardID") REFERENCES "UserCard"("CardID") ON DELETE CASCADE
);

-----------------------------------------------------------------------------

-- Create a function to log card status changes on updates only
CREATE OR REPLACE FUNCTION log_card_status_change()
RETURNS TRIGGER AS $$
BEGIN
    -- Only log when CardStatus actually changes
    IF OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus" THEN
        INSERT INTO "CardUpdates" ("CardID", "PreviousStatus", "NewStatus", "UpdatedAt")
        VALUES (
            NEW."CardID",
            OLD."CardStatus",
            NEW."CardStatus",
            CURRENT_TIMESTAMP
        );
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger for UPDATE operations only
CREATE OR REPLACE TRIGGER trg_card_status_update
AFTER UPDATE OF "CardStatus" ON "UserCard"
FOR EACH ROW
WHEN (OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus")
EXECUTE FUNCTION log_card_status_change();

-------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------

-- Create a function to update the LastUpdate column in UserCard when CardUpdates is updated
CREATE OR REPLACE FUNCTION update_usercard_lastupdate()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the LastUpdate column in UserCard with the latest UpdatedAt from CardUpdates
    UPDATE "UserCard"
    SET "LastUpdate" = NEW."UpdatedAt"
    WHERE "CardID" = NEW."CardID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger to update LastUpdate after insert on CardUpdates
CREATE OR REPLACE TRIGGER trg_update_usercard_lastupdate
AFTER INSERT ON "CardUpdates"
FOR EACH ROW
EXECUTE FUNCTION update_usercard_lastupdate();

-- Update existing UserCard records with the latest UpdatedAt from CardUpdates
UPDATE "UserCard" uc
SET "LastUpdate" = (
    SELECT MAX(cu."UpdatedAt")
    FROM "CardUpdates" cu
    WHERE cu."CardID" = uc."CardID"
)
WHERE EXISTS (
    SELECT 1 FROM "CardUpdates" cu 
    WHERE cu."CardID" = uc."CardID"
);

-- For UserCard records without any CardUpdates, set LastUpdate to CreatedAt (if not already set)
UPDATE "UserCard"
SET "LastUpdate" = "CreatedAt"
WHERE "LastUpdate" IS NULL;

-------------------------------------------------------------------------------------------
-------------------------------------Transaction-------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "Transaction" (
    "TransactionID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "TransferTransactionID" INT,
    "Amount" DECIMAL(10, 2) NOT NULL,
    "TransactionType" VARCHAR(20) NOT NULL CHECK ("TransactionType" IN ('Pay', 'Load', 'Refund', 'TransferOut', 'TransferIn')),
    "Description" TEXT,
    "TransactionDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "TransactionStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK ("TransactionStatus" IN ('Pending', 'Approved', 'Denied')),
    FOREIGN KEY ("CardID") REFERENCES "UserCard"("CardID") ON DELETE CASCADE,
    FOREIGN KEY ("TransferTransactionID") REFERENCES "Transaction"("TransactionID") ON DELETE CASCADE
);

---------------------------------------------------------------------------------------------------

-- Trigger function to control if a transaction can go through or not
-- It executes everytime there is a new transaction. 
CREATE OR REPLACE FUNCTION process_transaction_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_current_balance DECIMAL(10, 2);
	v_current_card_status VARCHAR(20);
BEGIN
    -- For new transactions with status 'Pending'
    IF NEW."TransactionStatus" = 'Pending' THEN
        -- Get the current balance
        SELECT "Balance" INTO v_current_balance
        FROM "UserCard"
        WHERE "CardID" = NEW."CardID"
        FOR UPDATE;  -- Lock the row to prevent race conditions

		SELECT "CardStatus" INTO v_current_card_status
		FROM "UserCard"
		WHERE "CardID" = NEW."CardID"
		FOR UPDATE;

        -- Process based on transaction type
        -- Pay & TransferOut transactions
        IF NEW."TransactionType" = 'Pay' OR NEW."TransactionType" = 'TransferOut' THEN
            -- Check if balance is sufficient
            IF NEW."Amount" > 0 AND v_current_balance >= NEW."Amount" AND v_current_card_status = 'Active' THEN
                -- Update card balance
                UPDATE "UserCard"
                SET "Balance" = "Balance" - NEW."Amount"
                WHERE "CardID" = NEW."CardID";
                
                -- Update transaction status
                NEW."TransactionStatus" := 'Approved';
            ELSE
                NEW."TransactionStatus" := 'Denied';
            END IF;
        -- Load transactions, card status must 'Active' or 'Inactive'
        ELSIF NEW."TransactionType" = 'Load' AND (v_current_card_status = 'Active' OR v_current_card_status = 'Inactive') THEN
            -- For load transactions, just need positive amount 
            IF NEW."Amount" > 0 THEN
                -- Update card balance
                UPDATE "UserCard"
                SET "Balance" = "Balance" + NEW."Amount"
                WHERE "CardID" = NEW."CardID";
                
                NEW."TransactionStatus" := 'Approved';
                
                -- Inactive cards can be activated with a 'Load'
                -- Cards are created as 'Inactive', so we need to activate them after the first load transaction
                IF v_current_card_status = 'Inactive' THEN
                    UPDATE "UserCard"
                    SET "CardStatus" = 'Active'
                    WHERE "CardID" = NEW."CardID";
                END IF;
            ELSE
                NEW."TransactionStatus" := 'Denied';
            END IF;
        -- Refund & TransferIn transaction
        ELSIF NEW."TransactionType" = 'Refund' OR NEW."TransactionType" = 'TransferIn' THEN
            -- For refund & transfer in transactions, just need positive amount and active card
            IF NEW."Amount" > 0 AND v_current_card_status = 'Active' THEN
                -- Update card balance
                UPDATE "UserCard"
                SET "Balance" = "Balance" + NEW."Amount"
                WHERE "CardID" = NEW."CardID";
                NEW."TransactionStatus" := 'Approved';
            ELSE
                NEW."TransactionStatus" := 'Denied';
            END IF;
        -- Any other transaction type and/or problem
        ELSE
            NEW."TransactionStatus" := 'Denied';
        END IF; 
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER process_transaction_trigger
BEFORE INSERT ON "Transaction"
FOR EACH ROW
WHEN (NEW."TransactionStatus" = 'Pending')
EXECUTE FUNCTION process_transaction_trigger();


-------------------------------------------------------------------------------------------
-------------------------------------PasswordHistory---------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "PasswordHistory" (
    "ID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "PasswordHash" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_PasswordHistory_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);

-- Create index for faster queries
CREATE INDEX IDX_PASSWORDHISTORY_CUSTOMERID ON "PasswordHistory"("CustomerID");
CREATE INDEX IDX_PASSWORDHISTORY_CREATEDAT ON "PasswordHistory"("CreatedAt");

-- Function to manage password history
CREATE OR REPLACE FUNCTION manage_password_history()
RETURNS TRIGGER AS $$
BEGIN
    -- Delete oldest password history if customer has 3 or more entries
    DELETE FROM "PasswordHistory"
    WHERE "ID" IN (
        SELECT "ID"
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
-----------------------------------Revoke Previous Tokens Function--------------------------

-- Create function to revoke previous tokens for a customer
CREATE OR REPLACE FUNCTION revoke_previous_tokens()
RETURNS TRIGGER AS $$
BEGIN
    -- Update all previous active tokens for this customer to be revoked
    UPDATE "SessionToken"
    SET "IsRevoked" = true
    WHERE "CustomerID" = NEW."CustomerID"
    AND "SessionID" != NEW."SessionID"
    AND "IsRevoked" = false;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger to revoke previous tokens
CREATE TRIGGER revoke_previous_tokens_trigger
AFTER INSERT ON "SessionToken"
FOR EACH ROW
EXECUTE FUNCTION revoke_previous_tokens();

-------------------------------------------------------------------------------------------
---------------------------------------Indexes---------------------------------------------
-------------------------------------------------------------------------------------------

-- Indexes for faster queries
CREATE INDEX IDX_CUSTOMER_CUSTOMERNUMBER ON "Customers"("CustomerNumber");
CREATE INDEX IDX_CUSTOMER_EMAIL ON "Customers"("Email");
CREATE INDEX IDX_USERCARD_CUSTOMERID ON "UserCard"("CustomerID");
CREATE INDEX IDX_USERCARD_CARDNUMBER ON "UserCard"("CardNumber");
CREATE INDEX IDX_TRANSACTION_CARDID ON "Transaction"("CardID");
CREATE INDEX IDX_TRANSACTION_TRANSFERTRANSACTIONID ON "Transaction"("TransferTransactionID");
CREATE INDEX IDX_SessionToken_CustomerID ON "SessionToken" ("CustomerID");
CREATE INDEX IDX_SessionToken_RefreshToken ON "SessionToken" ("RefreshToken");
CREATE INDEX IDX_SessionToken_AccessToken ON "SessionToken" ("AccessToken");
CREATE INDEX IDX_SessionToken_Expirations ON "SessionToken" ("AccessTokenExpiration", "RefreshTokenExpiration");
CREATE INDEX IDX_TOKENBLACKLIST_REFRESHTOKEN ON "TokenBlacklist"("RefreshToken");
CREATE INDEX IDX_TOKENBLACKLIST_BLACKLISTEDAT ON "TokenBlacklist"("BlacklistedAt");
CREATE INDEX IDX_CARDUPDATES_CARDID ON "CardUpdates"("CardID");
CREATE INDEX IDX_CARDUPDATES_UPDATEDAT ON "CardUpdates"("UpdatedAt");