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
    -- True if the user has verified their email
    -- However, for simplicity during early development, we will not implement email verification
    -- We will set this true. Later on, it will be set to false and email verification will be required
    "VerifiedUser" BOOLEAN NOT NULL DEFAULT TRUE, 
    "PasswordHash" VARCHAR(200) NOT NULL
);

---------------------------------------------------------------------------

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

CREATE OR REPLACE FUNCTION set_customer_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW."CustomerNumber" = 'New_Custmr' THEN
        NEW."CustomerNumber" := generate_customer_number();
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER set_customer_number_trigger
BEFORE INSERT ON "Customers"
FOR EACH ROW
WHEN (NEW."CustomerNumber" = 'New_Custmr')
EXECUTE FUNCTION set_customer_number();