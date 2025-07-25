-- Transaction tablosu oluşturma
CREATE TABLE Transaction (
    TransactionID INT PRIMARY KEY ,
    CardID INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    TransactionType VARCHAR(20) NOT NULL,
    Description TEXT,
    TransactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    TransactionStatus VARCHAR(20) DEFAULT 'Pending',
    FOREIGN KEY (CardID) REFERENCES UserCard(CardID)
);
-- Function to control if a transaction can go through or not
CREATE OR REPLACE FUNCTION process_transaction_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_current_balance DECIMAL(10, 2);
BEGIN
    -- For new transactions with status 'Pending'
    IF NEW.TransactionStatus = 'Pending' THEN
        -- Get the current balance
        SELECT Balance INTO v_current_balance
        FROM "UserCard"
        WHERE "CardID" = NEW.CardID
        FOR UPDATE;  -- Lock the row to prevent race conditions
 
        -- Process based on transaction type
        IF NEW.TransactionType = 'Pay' THEN
            -- Check if balance is sufficient
            IF v_current_balance >= NEW.Amount AND NEW.Amount > 0 THEN
                -- Update card balance
                UPDATE "UserCard"
                SET Balance = Balance - NEW.Amount
                WHERE "CardID" = NEW.CardID;
               
                -- Update transaction status
                NEW.TransactionStatus := 'Approved';
            ELSE
                NEW.TransactionStatus := 'Denied';
            END IF;
        ELSIF NEW.TransactionType = 'Load' THEN
            -- For load transactions, just need positive amount
            IF NEW.Amount > 0 THEN
                -- Update card balance
                UPDATE "UserCard"
                SET Balance = Balance + NEW.Amount
                WHERE "CardID" = NEW.CardID;
               
                NEW.TransactionStatus := 'Approved';
            ELSE
                NEW.TransactionStatus := 'Denied';
            END IF;
        END IF;
    END IF;
 
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
 
-- Create the trigger
CREATE TRIGGER tr_process_transaction
BEFORE INSERT ON "Transaction"
FOR EACH ROW
WHEN (NEW.TransactionStatus = 'Pending')
EXECUTE FUNCTION process_transaction_trigger();