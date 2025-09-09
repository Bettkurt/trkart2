-------------------------------------------------------------------------------------------
-----------------------------------SecurityEvents------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "SecurityEvents" (
    "SecurityEventID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER,
    "Email" VARCHAR(100),
    "EventType" VARCHAR(50) NOT NULL,
    "EventSeverity" VARCHAR(20) NOT NULL,
    "EventDetails" TEXT,
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "DeviceFingerprint" VARCHAR(255),
    "GeographicLocation" VARCHAR(100),
    "IsResolved" BOOLEAN DEFAULT FALSE,
    "ResolvedAt" TIMESTAMPTZ,
    "ResolvedBy" VARCHAR(50),
    "ResolutionNotes" TEXT,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_SecurityEvents_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function to update LastUsedAt in SessionToken
CREATE OR REPLACE FUNCTION update_session_last_used()
RETURNS TRIGGER AS $$
BEGIN
    NEW."LastUsedAt" = NOW();
    NEW."UsageCount" = COALESCE(NEW."UsageCount", 0) + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-------------------------------------------------------------------------------------------

-- Trigger for UpdatedAt columns
CREATE TRIGGER "trg_update_customers_updated_at"
    BEFORE UPDATE ON "Customers"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER "trg_update_sessiontoken_updated_at"
    BEFORE UPDATE ON "SessionToken"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Trigger for SessionToken usage tracking
CREATE TRIGGER "trg_update_sessiontoken_usage"
    BEFORE UPDATE ON "SessionToken"
    FOR EACH ROW EXECUTE FUNCTION update_session_last_used();