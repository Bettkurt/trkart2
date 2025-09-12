-------------------------------------------------------------------------------------------
-------------------------------------CardBlacklist-----------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "CardBlacklist" (
    "CardBlacklistID" SERIAL PRIMARY KEY,
    "CustomerID" INT NOT NULL,       -- Direct reference to customer (FK)
    -- Original CardID for reference 
    -- Will not work as FK since cards will moved completely to this table from UserCard table
    "OriginalCardID" INT NOT NULL,  
    "CardNumber" CHAR(16) NOT NULL,  -- Copy of the card number
    -- Leftover balance 
    -- TODO: Can later be sent to another Active card of the same user
    -- Add a `JOB` to this automatically, maybe check once a week
    "LeftOverBalance" DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    "CardType" INT NOT NULL,         -- Copy of card type
    "CardExpirationDate" DATE NOT NULL,  -- Original expiration date
    "OriginalCreatedAt" TIMESTAMP NOT NULL,   -- When the card was originally created
    -- 0: Deactivated, 1: Expired, 2: Reported Lost for more than 7 days
    -- TODO: Add a function/trigger to work 1 week after a card is added to this list with Reason 2
    -- and change it to Reason 0 and change CardStatus to 0 (Deactivated) in UserCard table.
    -- It will also update the CardUpdates table with the new status, automatically.
    "Reason" INT NOT NULL CHECK ("Reason" BETWEEN 0 AND 2),
    -- Who blacklisted the card. CustomerID or 0 for system
    -- "BlacklistedBy" INTEGER,
    "BlacklistedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- When the card was blacklisted
    "Notes" TEXT,                    -- Any additional notes
    
    CONSTRAINT "FK_CardBlacklist_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID") 
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_CardBlacklist_UserCard_OriginalCardID"
        FOREIGN KEY ("OriginalCardID") 
        REFERENCES "UserCard"("CardID") 
        ON DELETE CASCADE
);

-------------------------------------Update UserCard---------------------------------------

-- First, create a function that will be called by the trigger
CREATE OR REPLACE FUNCTION update_user_card_blacklist_status()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the UserCard table to mark the card as blacklisted
    -- and set the BlacklistedAt to the current timestamp
    UPDATE "UserCard"
    SET 
        "IsBlacklisted" = TRUE,
        "BlacklistedAt" = NEW."BlacklistedAt"
    WHERE "CardID" = NEW."OriginalCardID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger that fires after an insert on CardBlacklist
CREATE OR REPLACE TRIGGER tr_after_card_blacklist_insert
AFTER INSERT ON "CardBlacklist"
FOR EACH ROW
EXECUTE FUNCTION update_user_card_blacklist_status();