CREATE TABLE "Transaction" (
    "TransactionID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "TransferTransactionID" INT NOT NULL,
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
        ELSIF NEW."TransactionType" = 'Load' THEN
            -- For load transactions, just need positive amount
            IF NEW."Amount" > 0 THEN
                -- Update card balance
                UPDATE "UserCard"
                SET "Balance" = "Balance" + NEW."Amount"
                WHERE "CardID" = NEW."CardID";
                
                NEW."TransactionStatus" := 'Approved';
                
                -- Cards are created as 'Inactive', so we need to activate them after the first load transaction
                IF v_current_card_status = 'Inactive' THEN
                    UPDATE "UserCard"
                    SET "CardStatus" = 'Active'
                    WHERE "CardID" = NEW."CardID";
                END IF;
            ELSE
                NEW."TransactionStatus" := 'Denied';
            END IF;
        ELSIF NEW."TransactionType" = 'Refund' OR NEW."TransactionType" = 'TransferIn' THEN
            -- For refund transactions, just need positive amount
            IF NEW."Amount" > 0 AND v_current_card_status = 'Active' THEN
                -- Update card balance
                UPDATE "UserCard"
                SET "Balance" = "Balance" + NEW."Amount"
                WHERE "CardID" = NEW."CardID";
                NEW."TransactionStatus" := 'Approved';
            ELSE
                NEW."TransactionStatus" := 'Denied';
            END IF;           
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