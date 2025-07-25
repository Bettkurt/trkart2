CREATE TABLE "Customers" (
    "CustomerID" SERIAL PRIMARY KEY,
    "CustomerNumber" CHAR(10) NOT NULL UNIQUE DEFAULT 'New_Customer',
    "FullName" VARCHAR(100),
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    "VerifiedUser" BOOLEAN NOT NULL DEFAULT FALSE,
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

CREATE TRIGGER set_customer_number_trigger
BEFORE INSERT ON "Customers"
FOR EACH ROW
WHEN (NEW."CustomerNumber" = 'New_Customer')
EXECUTE FUNCTION generate_customer_number();