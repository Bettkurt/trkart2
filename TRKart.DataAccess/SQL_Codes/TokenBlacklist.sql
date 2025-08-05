-------------------------------------------------------------------------------------------
-------------------------------------TokenBlacklist---------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "TokenBlacklist" (
    "BlacklistID" SERIAL PRIMARY KEY,
    "RefreshToken" VARCHAR(500) NOT NULL,
    "BlacklistedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Reason" TEXT,
    "IPAddress" VARCHAR(45)
);

---------------------------------------Functions-------------------------------------------

-- Create function to purge expired tokens
CREATE OR REPLACE FUNCTION purge_expired_tokens()
RETURNS void AS $$
DECLARE
    purge_before TIMESTAMP;
BEGIN
    -- Remove sessions that expired more than 30 days ago
    purge_before := NOW() - INTERVAL '30 days';

    DELETE FROM "SessionToken"
    WHERE "RefreshTokenExpiration" < purge_before;

    -- Remove blacklist entries older than 90 days
    purge_before := NOW() - INTERVAL '90 days';

    DELETE FROM "TokenBlacklist"
    WHERE "BlacklistedAt" < purge_before;

    RAISE NOTICE 'Token cleanup completed at %', NOW();
END;
$$ LANGUAGE plpgsql;