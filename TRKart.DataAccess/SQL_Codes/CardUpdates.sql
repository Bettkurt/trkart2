CREATE TABLE "CardUpdates" (
    "UpdateID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "PreviousStatus" VARCHAR(20) NOT NULL,
    "NewStatus" VARCHAR(20) NOT NULL,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CardID") REFERENCES "UserCard"("CardID") ON DELETE CASCADE
);

-----------------------------------------------------------------------------

-- Create a function to log card status changes on updates only
CREATE OR REPLACE FUNCTION log_card_status_change()
RETURNS TRIGGER AS $$
BEGIN
    -- Only log when CardStatus actually changes
    IF OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus" THEN
        INSERT INTO "CardUpdates" ("CardID", "PreviousStatus", "NewStatus", "UpdatedAt")
        VALUES (
            NEW."CardID",
            OLD."CardStatus",
            NEW."CardStatus",
            CURRENT_TIMESTAMP
        );
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger for UPDATE operations only
CREATE OR REPLACE TRIGGER trg_card_status_update
AFTER UPDATE OF "CardStatus" ON "UserCard"
FOR EACH ROW
WHEN (OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus")
EXECUTE FUNCTION log_card_status_change();

------------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------------

-- Create a function to update the LastUpdate column in UserCard when CardUpdates is updated
CREATE OR REPLACE FUNCTION update_usercard_lastupdate()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the LastUpdate column in UserCard with the latest UpdatedAt from CardUpdates
    UPDATE "UserCard"
    SET "LastUpdate" = NEW."UpdatedAt"
    WHERE "CardID" = NEW."CardID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger to update LastUpdate after insert on CardUpdates
CREATE OR REPLACE TRIGGER trg_update_usercard_lastupdate
AFTER INSERT ON "CardUpdates"
FOR EACH ROW
EXECUTE FUNCTION update_usercard_lastupdate();

-- Update existing UserCard records with the latest UpdatedAt from CardUpdates
UPDATE "UserCard" uc
SET "LastUpdate" = (
    SELECT MAX(cu."UpdatedAt")
    FROM "CardUpdates" cu
    WHERE cu."CardID" = uc."CardID"
)
WHERE EXISTS (
    SELECT 1 FROM "CardUpdates" cu 
    WHERE cu."CardID" = uc."CardID"
);

-- For UserCard records without any CardUpdates, set LastUpdate to CreatedAt (if not already set)
UPDATE "UserCard"
SET "LastUpdate" = "CreatedAt"
WHERE "LastUpdate" IS NULL;
