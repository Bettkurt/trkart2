-------------------------------------------------------------------------------------------
----------------------------------------CardUpdates----------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "CardUpdates" (
    "UpdateID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    "PreviousStatus" INT,
    "NewStatus" INT,
    "StatusUpdatedAt" TIMESTAMPTZ,
    "PreviousType" INT,
    "NewType" INT,
    "TypeUpdatedAt" TIMESTAMPTZ,

    CONSTRAINT "FK_CardUpdates_UserCard_CardID"
    FOREIGN KEY ("CardID") 
    REFERENCES "UserCard"("CardID") 
    ON DELETE CASCADE
);

-------------------------------Log Card Status & Type Changes-------------------------------

-- Create a function to log card status changes on updates only
CREATE OR REPLACE FUNCTION log_card_update()
RETURNS TRIGGER AS $$
BEGIN
    -- Log status change
    IF OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus" THEN
        INSERT INTO "CardUpdates" ("CardID", "PreviousStatus", "NewStatus", "StatusUpdatedAt")
        VALUES (
            NEW."CardID",
            OLD."CardStatus",
            NEW."CardStatus",
            NOW()
        );
    END IF;
    
    -- Log card type change
    IF OLD."CardType" IS DISTINCT FROM NEW."CardType" THEN
        INSERT INTO "CardUpdates" ("CardID", "PreviousType", "NewType", "TypeUpdatedAt")
        VALUES (
            NEW."CardID",
            OLD."CardType",
            NEW."CardType",
            NOW()
        );
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger for status updates
CREATE OR REPLACE TRIGGER trg_card_update
AFTER UPDATE OF "CardStatus", "CardType" ON "UserCard"
FOR EACH ROW
WHEN (OLD."CardStatus" IS DISTINCT FROM NEW."CardStatus" 
      OR OLD."CardType" IS DISTINCT FROM NEW."CardType")
EXECUTE FUNCTION log_card_update();

-------------------------------------------------------------------------------------------

-- Function to update the LastUpdate column in UserCard when CardUpdates is updated
CREATE OR REPLACE FUNCTION update_usercard_lastupdate()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the LastUpdate column in UserCard with the latest update time
    UPDATE "UserCard"
    SET 
        "LastUpdate" = COALESCE(NEW."StatusUpdatedAt", NEW."TypeUpdatedAt"),
        "UpdateReason" = CASE 
            WHEN NEW."StatusUpdatedAt" IS NOT NULL AND NEW."TypeUpdatedAt" IS NOT NULL THEN 'Status and type change'
            WHEN NEW."StatusUpdatedAt" IS NOT NULL THEN 'Status change'
            WHEN NEW."TypeUpdatedAt" IS NOT NULL THEN 'Type change'
            ELSE 'Update'
        END
    WHERE "CardID" = NEW."CardID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger to update LastUpdate after insert on CardUpdates
CREATE OR REPLACE TRIGGER trg_update_usercard_lastupdate
AFTER INSERT ON "CardUpdates"
FOR EACH ROW
EXECUTE FUNCTION update_usercard_lastupdate();