CREATE TABLE UserCards (
    CardId SERIAL PRIMARY KEY,
    -- CardNumber CHAR(16) because it will always be 16 characters long
    CardNumber CHAR(16) NOT NULL UNIQUE,
    UserId INT NOT NULL,
    Balance DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Customers(Id) ON DELETE CASCADE
);

-- Random and unique card number generator function (16 digits; numbers and uppercase letters only)
CREATE OR REPLACE FUNCTION public.generate_unique_card_number() 
RETURNS char(16) AS $$
DECLARE
    chars text := 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    result varchar(16) := '';
    i integer := 0;
BEGIN
    WHILE i < 16 LOOP
        result := result || substr(chars, floor(random() * length(chars) + 1)::integer, 1);
        i := i + 1;
    END LOOP;
    
    -- Recursively try again if number already exists
    IF EXISTS (SELECT 1 FROM usercards WHERE cardnumber = result) THEN
        RETURN generate_unique_card_number();
    END IF;
    
    RETURN result;
END;
$$ LANGUAGE plpgsql;