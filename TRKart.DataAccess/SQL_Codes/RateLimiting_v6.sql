-------------------------------------------------------------------------------------------
-------------------------------------RateLimiting------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "RateLimiting" (
    "RateLimitID" SERIAL PRIMARY KEY,
    "Identifier" VARCHAR(255) NOT NULL,                    -- IP address, user ID, or other identifier
    "IdentifierType" VARCHAR(20) NOT NULL,                 -- IP, USER_ID, DEVICE_ID, etc.
    "Endpoint" VARCHAR(100) NOT NULL,                      -- API endpoint being rate limited
    "CustomerID" INTEGER,                                  -- Customer ID (optional for IP-based rate limiting)
    "RequestCount" INTEGER NOT NULL DEFAULT 1,             -- Current request count in this cycle
    "FirstRequestAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),   -- When first request in this cycle was made
    "LastRequestAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),    -- When last request in this cycle was made
    "IsBlocked" BOOLEAN NOT NULL DEFAULT false,            -- Whether this identifier is currently blocked
    "BlockedUntil" TIMESTAMPTZ,                            -- When the block expires
    "BlockReason" VARCHAR(500),                            -- Reason for the block
    "ViolationCount" INTEGER DEFAULT 0,                    -- Number of times rate limit was hit
    "FirstViolationAt" TIMESTAMPTZ,                        -- When first violation occurred
    "LastViolationAt" TIMESTAMPTZ,                         -- When last violation occurred
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_RateLimiting_Customers" 
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") 
    ON DELETE CASCADE
);

-------------------------------------------------------------------------------------------

-- Create trigger function for automatic UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_rate_limiting_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger
CREATE TRIGGER trg_rate_limiting_updated_at
    BEFORE UPDATE ON "RateLimiting"
    FOR EACH ROW
    EXECUTE FUNCTION update_rate_limiting_updated_at();