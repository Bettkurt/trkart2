-------------------------------------------------------------------------------------------
-------------------------------------Transaction-------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "Transaction" (
    "TransactionID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "TransferTransactionID" INT,
    "Amount" DECIMAL(10, 2) NOT NULL,
    "TransactionType" VARCHAR(20) NOT NULL CHECK ("TransactionType" 
        IN ('Pay', 'Load', 'Refund',
            'TransferOut', 'TransferIn', 
            'SystemTransferOut', 'SystemTransferIn')),
    "Description" TEXT,
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
    v_current_balance DECIMAL(10, 2);
	v_current_card_status INT;
BEGIN
-- Remember, CardStatus: 0 = Deactivated, 1 = Expired, 2 = Lost, 3 = Inactive, 4 = Active
    IF NEW."Amount" <= 0 THEN
        RAISE EXCEPTION 'Amount must be greater than 0';
    END IF;
    
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

        --IF v_current_card_status < 3 THEN
        --    RAISE EXCEPTION 'Card is not suitable for use';
        --END IF;

        -- Process based on transaction type
        -- Pay & TransferOut transactions
        IF NEW."TransactionType" = 'Pay' OR NEW."TransactionType" = 'TransferOut' THEN
            -- Check if balance is sufficient
            IF v_current_balance >= NEW."Amount" AND v_current_card_status = 4 THEN
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
        ELSIF NEW."TransactionType" = 'Load' AND v_current_card_status >= 3 THEN
            -- For load transactions, just need positive amount 
                -- Update card balance
                UPDATE "UserCard"
                SET "Balance" = "Balance" + NEW."Amount"
                WHERE "CardID" = NEW."CardID";
                
                NEW."TransactionStatus" := 'Approved';
                
                -- Inactive cards can be activated with a 'Load'
                -- Cards are created as 'Inactive', so we need to activate them after the first load transaction
                IF v_current_card_status = 3 THEN
                    UPDATE "UserCard"
                    SET "CardStatus" = 4
                    WHERE "CardID" = NEW."CardID";
                END IF;
        -- Refund & TransferIn transaction
        ELSIF NEW."TransactionType" = 'Refund' OR NEW."TransactionType" = 'TransferIn' THEN
            -- For refund & transfer in transactions, just need positive amount and active card
            IF v_current_card_status = 4 THEN
                -- Update card balance
                UPDATE "UserCard"
                SET "Balance" = "Balance" + NEW."Amount"
                WHERE "CardID" = NEW."CardID";
                NEW."TransactionStatus" := 'Approved';
            ELSE
                NEW."TransactionStatus" := 'Denied';
            END IF;
        -- Any other transaction type and/or problem
        ELSEIF NEW."TransactionType" = 'SystemTransferOut' THEN
            NEW."TransactionStatus" := 'Approved';
            -- Update card balance
            UPDATE "UserCard"
            SET "Balance" = "Balance" - NEW."Amount"
            WHERE "CardID" = NEW."CardID";
        ELSEIF NEW."TransactionType" = 'SystemTransferIn' THEN
            NEW."TransactionStatus" := 'Approved';
            -- Update card balance
            UPDATE "UserCard"
            SET "Balance" = "Balance" + NEW."Amount"
            WHERE "CardID" = NEW."CardID";
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