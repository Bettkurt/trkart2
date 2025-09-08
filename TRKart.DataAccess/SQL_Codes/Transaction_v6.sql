-------------------------------------------------------------------------------------------
-------------------------------------Transaction-------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "Transaction" (
    "TransactionID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "TransferTransactionID" INT,
    "Amount" DECIMAL(18, 2) NOT NULL,
    "FeeAmount" DECIMAL(18, 2) NULL DEFAULT 0.00,
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
WHEN (NEW."TransactionStatus" = 'Pending')
EXECUTE FUNCTION process_transaction_trigger();