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

------------------------------------No of Cards Constraint--------------------------------
-- Function to check if a customer has less than 3 cards
CREATE OR REPLACE FUNCTION check_issued_card_limit()
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
CREATE OR REPLACE TRIGGER enforce_issued_card_limit_trigger
BEFORE INSERT ON "UserCard"
FOR EACH ROW
EXECUTE FUNCTION check_issued_card_limit();