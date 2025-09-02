-------------------------------------------------------------------------------------------
------------------------------------Helper Functions---------------------------------------
-------------------------------------------------------------------------------------------

-- Function to calculate Luhn check digit (numeric part only) for both CardNumber and CustomerNumber 
CREATE OR REPLACE FUNCTION calculate_luhn_check_digit(number_str text) 
RETURNS int AS $$
DECLARE
    sum int := 0;
    digit int;
    is_second bool := false;
    i int;

BEGIN
    
    -- Process from right to left.
    FOR i IN REVERSE length(number_str)..1 LOOP
        digit := substring(number_str, i, 1)::int;
        
        IF is_second THEN
            digit := digit * 2;
            IF digit > 9 THEN
                digit := (digit / 10) + (digit % 10);
            END IF;
        END IF;
        
        sum := sum + digit;
        is_second := NOT is_second;
    END LOOP;
    
    -- Return the check digit that makes the sum a multiple of 10
    RETURN (10 - (sum % 10)) % 10;
END;
$$ LANGUAGE plpgsql;

-------------------------------------------------------------------------------------------
-------------------------------------Customers---------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "Customers" (
    "CustomerID" SERIAL PRIMARY KEY,
    "CustomerNumber" CHAR(10) NOT NULL UNIQUE DEFAULT 'New_Custmr',
    "FullName" VARCHAR(100),
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(200) NOT NULL
);

CREATE SEQUENCE customer_sequence START 1;

CREATE OR REPLACE FUNCTION generate_customer_number()
RETURNS CHAR(10) AS $$
DECLARE
    prefix text := 'C';
    sequence_num text;
    customer_base text;
    check_digit int;
BEGIN
    -- Get next sequence number (padded to 8 digits)
    sequence_num := lpad(nextval('customer_sequence')::text, 8, '0');
    
    -- Combine all parts except check digit (C + 8-digit sequence)
    customer_base := prefix || sequence_num;
    
    -- Calculate check digit using only numeric part (sequence)
    check_digit := calculate_luhn_check_digit(sequence_num);
    
    -- Return full customer number (10 characters total)
    RETURN customer_base || check_digit::text;
END;
$$ LANGUAGE plpgsql;

-- Function to set the customer number on insert
CREATE OR REPLACE FUNCTION set_customer_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW."CustomerNumber" = 'New_Custmr' THEN
        NEW."CustomerNumber" := generate_customer_number();
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER set_customer_number_trigger
BEFORE INSERT ON "Customers"
FOR EACH ROW
WHEN (NEW."CustomerNumber" = 'New_Custmr')
EXECUTE FUNCTION set_customer_number();

-------------------------------------------------------------------------------------------
-------------------------------------SessionToken------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "SessionToken" (
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INT NOT NULL,
    "Token" VARCHAR(500) UNIQUE NOT NULL,
    "Expiration" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP + INTERVAL '1 hour',  -- We can change this to 1 day or 1 week etc. if needed
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") ON DELETE CASCADE
);

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
    "CardStatus" VARCHAR(20) NOT NULL DEFAULT 'Inactive' CHECK ("CardStatus" IN ('Active', 'Inactive', 'Lost')), 
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Create a sequence for card numbers (10 digits)
CREATE SEQUENCE card_sequence START 1;

-- Function to generate the card number
CREATE OR REPLACE FUNCTION generate_card_number()
RETURNS char(16) AS $$
DECLARE
    prefix text := 'TRK';
    bin text := '90'; -- BIN starts with 90, other 6 are included in the sequence
    sequence_num text;
    card_base text;
    check_digit int;
BEGIN
    -- Get next sequence number (padded to 10 digits)
    sequence_num := lpad(nextval('card_sequence')::text, 10, '0');
    
    -- Combine all parts except check digit (TRK90 + 10-digit sequence)
    card_base := prefix || bin || sequence_num;
    
    -- Calculate check digit using only numeric part (90 + sequence)
    check_digit := calculate_luhn_check_digit(bin || sequence_num);
    
    -- Return full card number (16 characters total)
    RETURN card_base || check_digit::text;
END;
$$ LANGUAGE plpgsql;

-- Function to set the card number on insert
CREATE OR REPLACE FUNCTION set_card_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW."CardNumber" = 'Generate_New_Num' THEN
        NEW."CardNumber" := generate_card_number();
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER set_card_number_trigger
BEFORE INSERT ON "UserCard"
FOR EACH ROW
WHEN (NEW."CardNumber" = 'Generate_New_Num')
EXECUTE FUNCTION set_card_number();

----------------------------------No of Cards Limit Constraint-----------------------------
-- Function to check if a customer has less than 3 cards
CREATE OR REPLACE FUNCTION check_card_limit()
RETURNS TRIGGER AS $$
DECLARE
    card_count INTEGER;
BEGIN
    -- Count the number of cards for this customer
    SELECT COUNT(*) INTO card_count
    FROM "UserCard"
    WHERE "CustomerID" = NEW."CustomerID";

    -- If the customer already has 3 or more cards, prevent the insert
    IF card_count >= 3 THEN
        RAISE EXCEPTION 'A customer cannot have more than 3 cards';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger to enforce the card limit
CREATE OR REPLACE TRIGGER enforce_card_limit_trigger
BEFORE INSERT ON "UserCard"
FOR EACH ROW
EXECUTE FUNCTION check_card_limit();

-------------------------------------------------------------------------------------------
-------------------------------------Transaction-------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "Transaction" (
    "TransactionID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "Amount" DECIMAL(10, 2) NOT NULL,
    "TransactionType" VARCHAR(20) NOT NULL CHECK ("TransactionType" IN ('Pay', 'Load', 'Refund', 'TransferOut', 'TransferIn')),
    "Description" TEXT,
    "TransactionDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "TransactionStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK ("TransactionStatus" IN ('Pending', 'Approved', 'Denied')),
    FOREIGN KEY ("CardID") REFERENCES "UserCard"("CardID") ON DELETE CASCADE
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

-------------------------------------------------------------------------------------------
---------------------------------------Indexes---------------------------------------------
-------------------------------------------------------------------------------------------

CREATE INDEX IDX_CUSTOMER_CUSTOMERNUMBER ON "Customers"("CustomerNumber");
CREATE INDEX IDX_USERCARD_CUSTOMERID ON "UserCard"("CustomerID");
CREATE INDEX IDX_USERCARD_CARDNUMBER ON "UserCard"("CardNumber");
CREATE INDEX IDX_TRANSACTION_CARDID ON "Transaction"("CardID");
CREATE INDEX IDX_SESSIONTOKEN_CUSTOMERID ON "SessionToken"("CustomerID");