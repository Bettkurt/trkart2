
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
CREATE INDEX IDX_Customer_CustomerNumber ON "Customers"("CustomerNumber");
CREATE INDEX IDX_Customer_Email ON "Customers"("Email");
CREATE INDEX IDX_UserCard_CustomerID ON "UserCard"("CustomerID");
CREATE INDEX IDX_UserCard_CardNumber ON "UserCard"("CardNumber");
CREATE INDEX IDX_Transaction_CardID ON "Transaction"("CardID");
CREATE INDEX IDX_Transaction_TransferTransactionID ON "Transaction"("TransferTransactionID");
CREATE INDEX IDX_SessionToken_CustomerID ON "SessionToken" ("CustomerID");
CREATE INDEX IDX_SessionToken_RefreshToken ON "SessionToken" ("RefreshToken");
CREATE INDEX IDX_SessionToken_AccessToken ON "SessionToken" ("AccessToken");
CREATE INDEX IDX_SessionToken_Expirations ON "SessionToken" ("AccessTokenExpiration", "RefreshTokenExpiration");
CREATE INDEX IDX_TOKENBLACKLIST_REFRESHTOKEN ON "TokenBlacklist"("RefreshToken");
CREATE INDEX IDX_TOKENBLACKLIST_BLACKLISTEDAT ON "TokenBlacklist"("BlacklistedAt");
CREATE INDEX IDX_CARDUPDATES_CARDID ON "CardUpdates"("CardID");
CREATE INDEX IDX_CARDUPDATES_UPDATEDAT ON "CardUpdates"("UpdatedAt");

-------------------------------------------------------------------------------------------
-------------------------------------TopUp Migration--------------------------------------
-------------------------------------------------------------------------------------------
-- This script adds Top-Up functionality to the existing Transaction table
-- Includes: New columns for ExternalRef, PaymentMethod, FeeAmount, Note
-- Updates: Amount precision to decimal(18,2), TransactionType enum to include TopUp
-- Modifies: Trigger to handle TopUp transaction processing
-- Features: Idempotent design - safe to run multiple times

-- Step 1: Add new columns to Transaction table (only if they don't exist)
DO $$
BEGIN
    -- Add ExternalRef column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Transaction' AND column_name = 'ExternalRef'
    ) THEN
        ALTER TABLE "Transaction" ADD COLUMN "ExternalRef" VARCHAR(100) NULL;
        RAISE NOTICE 'Added ExternalRef column to Transaction table';
    ELSE
        RAISE NOTICE 'ExternalRef column already exists in Transaction table';
    END IF;
    
    -- Add PaymentMethod column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Transaction' AND column_name = 'PaymentMethod'
    ) THEN
        ALTER TABLE "Transaction" ADD COLUMN "PaymentMethod" VARCHAR(50) NULL;
        RAISE NOTICE 'Added PaymentMethod column to Transaction table';
    ELSE
        RAISE NOTICE 'PaymentMethod column already exists in Transaction table';
    END IF;
    
    -- Add FeeAmount column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Transaction' AND column_name = 'FeeAmount'
    ) THEN
        ALTER TABLE "Transaction" ADD COLUMN "FeeAmount" DECIMAL(18, 2) NULL DEFAULT 0.00;
        RAISE NOTICE 'Added FeeAmount column to Transaction table';
    ELSE
        RAISE NOTICE 'FeeAmount column already exists in Transaction table';
    END IF;
    
    -- Add Note column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Transaction' AND column_name = 'Note'
    ) THEN
        ALTER TABLE "Transaction" ADD COLUMN "Note" TEXT NULL;
        RAISE NOTICE 'Added Note column to Transaction table';
    ELSE
        RAISE NOTICE 'Note column already exists in Transaction table';
    END IF;
END $$;

-- Step 2: Create unique constraint on ExternalRef (only if it doesn't exist)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'IX_Transaction_ExternalRef'
    ) THEN
        CREATE UNIQUE INDEX "IX_Transaction_ExternalRef" 
        ON "Transaction" ("ExternalRef") 
        WHERE "ExternalRef" IS NOT NULL;
        RAISE NOTICE 'Created unique index on ExternalRef';
    ELSE
        RAISE NOTICE 'Unique index on ExternalRef already exists';
    END IF;
END $$;

-- Step 3: Update Amount precision to decimal(18,2)
DO $$
BEGIN
    -- Check if Amount column needs precision update
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Transaction' 
        AND column_name = 'Amount' 
        AND (numeric_precision != 18 OR numeric_scale != 2)
    ) THEN
        ALTER TABLE "Transaction" ALTER COLUMN "Amount" TYPE DECIMAL(18, 2);
        RAISE NOTICE 'Updated Amount column precision to DECIMAL(18,2)';
    ELSE
        RAISE NOTICE 'Amount column already has correct precision';
    END IF;
END $$;

-- Step 4: Update Balance precision in UserCard table
DO $$
BEGIN
    -- Check if Balance column needs precision update
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'UserCard' 
        AND column_name = 'Balance' 
        AND (numeric_precision != 18 OR numeric_scale != 2)
    ) THEN
        ALTER TABLE "UserCard" ALTER COLUMN "Balance" TYPE DECIMAL(18, 2);
        RAISE NOTICE 'Updated Balance column precision to DECIMAL(18,2)';
    ELSE
        RAISE NOTICE 'Balance column already has correct precision';
    END IF;
END $$;

-- Step 5: Handle TransactionType enum update safely
DO $$
DECLARE
    constraint_name_var VARCHAR(128);
    existing_check_clause TEXT;
BEGIN
    -- Find existing TransactionType check constraint
    SELECT tc.constraint_name, cc.check_clause
    INTO constraint_name_var, existing_check_clause
    FROM information_schema.table_constraints tc
    JOIN information_schema.check_constraints cc ON tc.constraint_name = cc.constraint_name
    WHERE tc.table_name = 'Transaction'
      AND tc.constraint_type = 'CHECK'
      AND cc.check_clause ILIKE '%TransactionType%'
    LIMIT 1;
    
    -- Check if TopUp is already in the constraint
    IF constraint_name_var IS NOT NULL AND existing_check_clause NOT ILIKE '%TopUp%' THEN
        -- Drop the existing constraint
        EXECUTE format('ALTER TABLE "Transaction" DROP CONSTRAINT "%s"', constraint_name_var);
        RAISE NOTICE 'Dropped existing TransactionType constraint: %', constraint_name_var;
        
        -- Add the new constraint with TopUp
        ALTER TABLE "Transaction" 
        ADD CONSTRAINT "CK_Transaction_TransactionType" 
        CHECK ("TransactionType" IN ('Pay', 'Load', 'Refund', 'TransferOut', 'TransferIn', 'TopUp'));
        RAISE NOTICE 'Added new TransactionType constraint with TopUp support';
        
    ELSIF constraint_name_var IS NULL THEN
        -- No constraint exists, create it
        ALTER TABLE "Transaction" 
        ADD CONSTRAINT "CK_Transaction_TransactionType" 
        CHECK ("TransactionType" IN ('Pay', 'Load', 'Refund', 'TransferOut', 'TransferIn', 'TopUp'));
        RAISE NOTICE 'Created new TransactionType constraint with TopUp support';
    ELSE
        RAISE NOTICE 'TransactionType constraint already includes TopUp support';
    END IF;
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Warning: Could not update TransactionType constraint: %', SQLERRM;
        -- Continue execution even if constraint update fails
END $$;

-- Step 6: Handle TransactionStatus enum update safely
DO $$
DECLARE
    constraint_name_var VARCHAR(128);
    existing_check_clause TEXT;
BEGIN
    -- Find existing TransactionStatus check constraint
    SELECT tc.constraint_name, cc.check_clause
    INTO constraint_name_var, existing_check_clause
    FROM information_schema.table_constraints tc
    JOIN information_schema.check_constraints cc ON tc.constraint_name = cc.constraint_name
    WHERE tc.table_name = 'Transaction'
      AND tc.constraint_type = 'CHECK'
      AND cc.check_clause ILIKE '%TransactionStatus%'
    LIMIT 1;
    
    -- Check if Expired is already in the constraint
    IF constraint_name_var IS NOT NULL AND existing_check_clause NOT ILIKE '%Expired%' THEN
        -- Drop the existing constraint
        EXECUTE format('ALTER TABLE "Transaction" DROP CONSTRAINT "%s"', constraint_name_var);
        RAISE NOTICE 'Dropped existing TransactionStatus constraint: %', constraint_name_var;
        
        -- Add the new constraint with Expired
        ALTER TABLE "Transaction" 
        ADD CONSTRAINT "CK_Transaction_TransactionStatus" 
        CHECK ("TransactionStatus" IN ('Pending', 'Approved', 'Denied', 'Expired'));
        RAISE NOTICE 'Added new TransactionStatus constraint with Expired status';
        
    ELSIF constraint_name_var IS NULL THEN
        -- No constraint exists, create it
        ALTER TABLE "Transaction" 
        ADD CONSTRAINT "CK_Transaction_TransactionStatus" 
        CHECK ("TransactionStatus" IN ('Pending', 'Approved', 'Denied', 'Expired'));
        RAISE NOTICE 'Created new TransactionStatus constraint with Expired status';
    ELSE
        RAISE NOTICE 'TransactionStatus constraint already includes Expired status';
    END IF;
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'Warning: Could not update TransactionStatus constraint: %', SQLERRM;
        -- Continue execution even if constraint update fails
END $$;

-- Step 7: Create or replace the trigger function to handle TopUp transactions
CREATE OR REPLACE FUNCTION process_transaction_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_current_balance DECIMAL(18, 2);
    v_current_card_status VARCHAR(20);
    v_net_amount DECIMAL(18, 2);
BEGIN
    -- For new transactions with status 'Pending'
    IF NEW."TransactionStatus" = 'Pending' THEN
        -- Get the current balance and card status with row-level locking
        SELECT "Balance", "CardStatus" 
        INTO v_current_balance, v_current_card_status
        FROM "UserCard"
        WHERE "CardID" = NEW."CardID"
        FOR UPDATE;  -- Lock the row to prevent race conditions

        -- Validate card exists
        IF v_current_balance IS NULL THEN
            NEW."TransactionStatus" := 'Denied';
            RETURN NEW;
        END IF;

        -- Process based on transaction type
        CASE NEW."TransactionType"
            -- Pay & TransferOut transactions (debit operations)
            WHEN 'Pay', 'TransferOut' THEN
                IF NEW."Amount" > 0 AND v_current_balance >= NEW."Amount" AND v_current_card_status = 'Active' THEN
                    -- Update card balance (subtract amount)
                    UPDATE "UserCard"
                    SET "Balance" = "Balance" - NEW."Amount"
                    WHERE "CardID" = NEW."CardID";
                    
                    NEW."TransactionStatus" := 'Approved';
                ELSE
                    NEW."TransactionStatus" := 'Denied';
                END IF;
            
            -- Load transactions (can reactivate inactive cards)
            WHEN 'Load' THEN
                IF NEW."Amount" > 0 AND (v_current_card_status = 'Active' OR v_current_card_status = 'Inactive') THEN
                    -- Update card balance (add amount)
                    UPDATE "UserCard"
                    SET "Balance" = "Balance" + NEW."Amount"
                    WHERE "CardID" = NEW."CardID";
                    
                    NEW."TransactionStatus" := 'Approved';
                    
                    -- Reactivate inactive cards
                    IF v_current_card_status = 'Inactive' THEN
                        UPDATE "UserCard"
                        SET "CardStatus" = 'Active'
                        WHERE "CardID" = NEW."CardID";
                    END IF;
                ELSE
                    NEW."TransactionStatus" := 'Denied';
                END IF;
            
            -- TopUp transactions (external payment with fees)
            WHEN 'TopUp' THEN
                -- Card must be Active for top-ups
                IF v_current_card_status != 'Active' THEN
                    NEW."TransactionStatus" := 'Denied';
                ELSIF NEW."Amount" <= 0 THEN
                    NEW."TransactionStatus" := 'Denied';
                ELSE
                    -- Calculate net amount (gross amount minus fee)
                    v_net_amount := NEW."Amount" - COALESCE(NEW."FeeAmount", 0);
                    
                    -- Net amount must be positive
                    IF v_net_amount > 0 THEN
                        -- Update card balance with net amount
                        UPDATE "UserCard"
                        SET "Balance" = "Balance" + v_net_amount
                        WHERE "CardID" = NEW."CardID";
                        
                        NEW."TransactionStatus" := 'Approved';
                    ELSE
                        NEW."TransactionStatus" := 'Denied';
                    END IF;
                END IF;
            
            -- Refund & TransferIn transactions (credit operations)
            WHEN 'Refund', 'TransferIn' THEN
                IF NEW."Amount" > 0 AND v_current_card_status = 'Active' THEN
                    -- Update card balance (add amount)
                    UPDATE "UserCard"
                    SET "Balance" = "Balance" + NEW."Amount"
                    WHERE "CardID" = NEW."CardID";
                    
                    NEW."TransactionStatus" := 'Approved';
                ELSE
                    NEW."TransactionStatus" := 'Denied';
                END IF;
            
            -- Unknown transaction type
            ELSE
                NEW."TransactionStatus" := 'Denied';
        END CASE;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Step 8: Ensure trigger exists on Transaction table
DO $$
BEGIN
    -- Check if trigger exists
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.triggers 
        WHERE trigger_name = 'transaction_processing_trigger' 
        AND event_object_table = 'Transaction'
    ) THEN
        -- Create the trigger
        CREATE TRIGGER transaction_processing_trigger
            BEFORE INSERT OR UPDATE ON "Transaction"
            FOR EACH ROW
            EXECUTE FUNCTION process_transaction_trigger();
        RAISE NOTICE 'Created transaction processing trigger';
    ELSE
        RAISE NOTICE 'Transaction processing trigger already exists';
    END IF;
END $$;

-- Step 9: Add performance indexes (only if they don't exist)
DO $$
BEGIN
    -- PaymentMethod index
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'IX_Transaction_PaymentMethod'
    ) THEN
        CREATE INDEX "IX_Transaction_PaymentMethod" ON "Transaction" ("PaymentMethod");
        RAISE NOTICE 'Created index on PaymentMethod';
    END IF;
    
    -- Transaction Type and Status composite index
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'IX_Transaction_Type_Status'
    ) THEN
        CREATE INDEX "IX_Transaction_Type_Status" ON "Transaction" ("TransactionType", "TransactionStatus");
        RAISE NOTICE 'Created composite index on TransactionType and TransactionStatus';
    END IF;
    
    -- Transaction Date index
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'IX_Transaction_Date'
    ) THEN
        CREATE INDEX "IX_Transaction_Date" ON "Transaction" ("TransactionDate");
        RAISE NOTICE 'Created index on TransactionDate';
    END IF;
    
    -- CardID and TransactionType composite index (for performance)
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'IX_Transaction_CardID_Type'
    ) THEN
        CREATE INDEX "IX_Transaction_CardID_Type" ON "Transaction" ("CardID", "TransactionType");
        RAISE NOTICE 'Created composite index on CardID and TransactionType';
    END IF;
END $$;

-- Step 10: Add column comments for documentation
DO $$
BEGIN
    -- Add comments only if columns exist
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'ExternalRef') THEN
        COMMENT ON COLUMN "Transaction"."ExternalRef" IS 'External payment provider transaction ID for idempotency and tracking';
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'PaymentMethod') THEN
        COMMENT ON COLUMN "Transaction"."PaymentMethod" IS 'Payment method used (e.g., CreditCard, DebitCard, Wire, Cash, PayPal)';
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'FeeAmount') THEN
        COMMENT ON COLUMN "Transaction"."FeeAmount" IS 'Fee charged by payment provider (deducted from gross amount)';
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'Note') THEN
        COMMENT ON COLUMN "Transaction"."Note" IS 'Optional note or description for the transaction';
    END IF;
    
    RAISE NOTICE 'Added column comments for documentation';
END $$;

-- Step 11: Validation and summary
DO $$
DECLARE
    v_external_ref_exists BOOLEAN;
    v_payment_method_exists BOOLEAN;
    v_fee_amount_exists BOOLEAN;
    v_note_exists BOOLEAN;
    v_trigger_exists BOOLEAN;
BEGIN
    -- Check if all new columns exist
    SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'ExternalRef') INTO v_external_ref_exists;
    SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'PaymentMethod') INTO v_payment_method_exists;
    SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'FeeAmount') INTO v_fee_amount_exists;
    SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Transaction' AND column_name = 'Note') INTO v_note_exists;
    
    -- Check if trigger exists
    SELECT EXISTS (SELECT 1 FROM information_schema.triggers WHERE trigger_name = 'transaction_processing_trigger' AND event_object_table = 'Transaction') INTO v_trigger_exists;
    
    -- Report status
    RAISE NOTICE '=====================================';
    RAISE NOTICE 'TopUp Migration Summary:';
    RAISE NOTICE '=====================================';
    RAISE NOTICE 'ExternalRef column: %', CASE WHEN v_external_ref_exists THEN 'EXISTS' ELSE 'MISSING' END;
    RAISE NOTICE 'PaymentMethod column: %', CASE WHEN v_payment_method_exists THEN 'EXISTS' ELSE 'MISSING' END;
    RAISE NOTICE 'FeeAmount column: %', CASE WHEN v_fee_amount_exists THEN 'EXISTS' ELSE 'MISSING' END;
    RAISE NOTICE 'Note column: %', CASE WHEN v_note_exists THEN 'EXISTS' ELSE 'MISSING' END;
    RAISE NOTICE 'Transaction trigger: %', CASE WHEN v_trigger_exists THEN 'EXISTS' ELSE 'MISSING' END;
    
    IF v_external_ref_exists AND v_payment_method_exists AND v_fee_amount_exists AND v_note_exists AND v_trigger_exists THEN
        RAISE NOTICE 'TopUp Migration: COMPLETED SUCCESSFULLY';
    ELSE
        RAISE NOTICE 'TopUp Migration: SOME COMPONENTS MISSING - Check logs above';
    END IF;
    RAISE NOTICE '=====================================';
END $$;


CREATE INDEX IDX_TokenBlacklist_SessionID ON "TokenBlacklist" ("SessionID");
CREATE INDEX IDX_TokenBlacklist_RefreshToken ON "TokenBlacklist"("RefreshToken");
CREATE INDEX IDX_TokenBlacklist_BlacklistedAt ON "TokenBlacklist"("BlacklistedAt");
CREATE INDEX IDX_CardUpdates_CardID ON "CardUpdates"("CardID");
CREATE INDEX IDX_CardUpdates_UpdatedAt ON "CardUpdates"("UpdatedAt");
