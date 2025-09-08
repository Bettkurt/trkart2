-------------------------------------------------------------------------------------------
-------------------------------------PasswordHistory---------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "PasswordHistory" (
    "PasswordHistoryID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "PasswordHash" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_PasswordHistory_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Function to manage password history
CREATE OR REPLACE FUNCTION manage_password_history()
RETURNS TRIGGER AS $$
BEGIN
    -- Delete oldest password history if customer has 3 or more entries
    DELETE FROM "PasswordHistory"
    WHERE "PasswordHistoryID" IN (
        SELECT "PasswordHistoryID"
        FROM "PasswordHistory"
        WHERE "CustomerID" = NEW."CustomerID"
        ORDER BY "CreatedAt" ASC
        LIMIT 1
    )
    AND (
        SELECT COUNT(*)
        FROM "PasswordHistory"
        WHERE "CustomerID" = NEW."CustomerID"
    ) >= 3;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER trg_manage_password_history
BEFORE INSERT ON "PasswordHistory"
FOR EACH ROW
EXECUTE FUNCTION manage_password_history();

-------------------------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION update_customer_password_changed_at()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE "Customers"
    SET "PasswordChangedAt" = NEW."CreatedAt"
    WHERE "CustomerID" = NEW."CustomerID";

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE OR REPLACE TRIGGER trg_update_customer_password_changed_at
AFTER INSERT ON "PasswordHistory" -- After insert. So, we only update it for actual password changes
FOR EACH ROW
EXECUTE FUNCTION update_customer_password_changed_at();