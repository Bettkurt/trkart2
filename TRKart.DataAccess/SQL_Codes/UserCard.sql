CREATE TABLE "UserCard" (
    "CardID" SERIAL PRIMARY KEY,
    -- CardNumber CHAR(16) because it will always be 16 characters long
    "CardNumber" CHAR(16) NOT NULL UNIQUE DEFAULT 'Generate_New_Num',
    "CustomerID" INT NOT NULL,
    "Balance" DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    -- 'Active' or 'Inactive'. We may add different status types in the future, 
    -- i.e. 'Lost', 'Suspended', 'Deleted' etc.; based on the feature requirements
    "CardStatus" VARCHAR(20) NOT NULL DEFAULT 'Inactive', 
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") ON DELETE CASCADE
);

---------------------------------------------------------------------------------------------------

-- Create a sequence for card numbers (10 digits)
CREATE SEQUENCE card_sequence START 1;

-- Function to calculate Luhn check digit (numeric only)
CREATE OR REPLACE FUNCTION calculate_luhn_check_digit(number_str text) 
RETURNS int AS $$
DECLARE
    sum int := 0;
    digit int;
    is_second bool := false;
    i int;

BEGIN
    
    -- Process from right to left. Also, do not include 'TRK' in the calculation.
    FOR i IN REVERSE length(number_str)..2 LOOP
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

-- Function to generate the card number
CREATE OR REPLACE FUNCTION generate_card_number()
RETURNS char(16) AS $$
DECLARE
    prefix text := 'TRK90';  -- TRK + BIN (90)
    sequence_num text;
    card_base text;
    check_digit int;
BEGIN
    -- Get next sequence number (padded to 10 digits)
    sequence_num := lpad(nextval('card_sequence')::text, 10, '0');
    
    -- Combine all parts except check digit (TRK90 + 10-digit sequence)
    card_base := prefix || sequence_num;
    
    -- Calculate check digit using only numeric part (9000000000 + sequence)
    check_digit := calculate_luhn_check_digit('90' || sequence_num);
    
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
CREATE TRIGGER trg_set_card_number
BEFORE INSERT ON "UserCard"
FOR EACH ROW
WHEN (NEW."CardNumber" = 'Generate_New_Num')
EXECUTE FUNCTION set_card_number();