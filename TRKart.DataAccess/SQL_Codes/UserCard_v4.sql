-------------------------------------------------------------------------------------------
-------------------------------------UserCard----------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "UserCard" (
    "CardID" SERIAL PRIMARY KEY,
    "CustomerID" INT NOT NULL,
    -- CardNumber CHAR(16) because it will always be 16 characters long
    "CardNumber" CHAR(16) NOT NULL UNIQUE DEFAULT 'Generate_New_Num',
    "Balance" DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    -- 0: Deactivated, 1: Expired, 2: Lost, 3: Inactive, 4: Active
    "CardStatus" INT NOT NULL DEFAULT 3 CHECK ("CardStatus" BETWEEN 0 AND 4),
    -- 0: Standart, 1: Gold, 2: Platinum
    "CardType" INT NOT NULL DEFAULT 0 CHECK ("CardType" BETWEEN 0 AND 2),
    "CardName" VARCHAR(20),
    -- Default is calculated by DB. 5 years from current date, and end of the current month
    "CardExpirationDate" DATE NOT NULL DEFAULT 
        (DATE_TRUNC('MONTH', CURRENT_DATE) + 
        INTERVAL '5 years' + 
        INTERVAL '1 month' - 
        INTERVAL '1 day')::DATE,
    "IsBlacklisted" BOOLEAN NOT NULL DEFAULT FALSE,
    "BlacklistedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "LastUpdate" TIMESTAMP,
    "UpdateReason" VARCHAR(100),

    CONSTRAINT "FK_UserCard_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID") 
        ON DELETE CASCADE
);

------------------------------------Number of Cards Constraint--------------------------------

-- Function to check if a customer has less than 3 cards
CREATE OR REPLACE FUNCTION check_issued_card_limit()
RETURNS TRIGGER AS $$
DECLARE
    card_count INTEGER;
BEGIN
    -- Count the number of active cards for this customer (excluding deactivated cards)
    SELECT COUNT(*) INTO card_count
    FROM "UserCard"
    WHERE "CustomerID" = NEW."CustomerID"
    -- 0: Deactivated, 1: Expired, 2: Lost, 3: Inactive, 4: Active
    -- Deactivated (As far as users concern they are deleted), and expired cards are not counted
    AND "CardStatus" > 2;

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