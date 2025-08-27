-------------------------------------------------------------------------------------------
-------------------------------------TokenBlacklist----------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "TokenBlacklist" (
    "BlacklistID" SERIAL PRIMARY KEY,
    "SessionID" INTEGER NOT NULL,
    "RefreshToken" VARCHAR(500) NOT NULL,
    "BlacklistedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Reason" TEXT,
    "IPAddress" TEXT,

    CONSTRAINT "FK_TokenBlacklist_SessionToken_SessionID"
        FOREIGN KEY ("SessionID") 
        REFERENCES "SessionToken"("SessionID") 
        ON DELETE CASCADE
);

---------------------Update IsRevoked in SessionToken When Blacklisted---------------------

CREATE OR REPLACE FUNCTION update_sessiontoken_isrevoked()
RETURNS TRIGGER AS $$
BEGIN
    -- Update the IsRevoked flag in the SessionToken table
    UPDATE "SessionToken"
    SET "IsRevoked" = TRUE
    WHERE "SessionID" = NEW."SessionID";
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE OR REPLACE TRIGGER trg_update_sessiontoken_isrevoked
AFTER INSERT ON "TokenBlacklist"
FOR EACH ROW
EXECUTE FUNCTION update_sessiontoken_isrevoked();