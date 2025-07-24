-- Function to generate a unique customer number
CREATE OR REPLACE FUNCTION generate_unique_customer_number()
RETURNS DECIMAL(8) AS $$
DECLARE
    v_customer_number DECIMAL(8);
BEGIN
    -- Generate a random number between 10000000 and 99999999
    v_customer_number := FLOOR(RANDOM() * (99999999 - 10000000 + 1)) + 10000000;
    
    -- Recursively try again if number already exists
    IF EXISTS (SELECT 1 FROM "Customers" WHERE "CustomerNumber" = v_customer_number) THEN
        RETURN generate_unique_customer_number();
    END IF;
    
    RETURN v_customer_number;
END;
$$ LANGUAGE plpgsql;

-----------------------------------------------------------------------------------------------

CREATE TABLE "Customers" (
    "CustomerID" SERIAL PRIMARY KEY,
    "CustomerNumber" DECIMAL(8) NOT NULL UNIQUE DEFAULT generate_unique_customer_number(),
    "FullName" VARCHAR(100),
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);