CREATE TABLE "SessionToken" (
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INT NOT NULL,
    "Token" VARCHAR(500) UNIQUE NOT NULL,
    "TimesUsed" INT NOT NULL DEFAULT 0,
    "IsValid" BOOLEAN NOT NULL DEFAULT TRUE,
    "Expiration" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP + INTERVAL '1 hour',  -- We can change this to 1 day or 1 week etc. if needed
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") ON DELETE CASCADE
);

-- Function to validate and update session token
CREATE OR REPLACE FUNCTION validate_session_token(
    p_token VARCHAR(500),
    p_password VARCHAR(255)
)
RETURNS TABLE (
    is_valid BOOLEAN,
    customer_id INT,
    message VARCHAR(255)
) AS $$
DECLARE
    v_customer_id INT;
    v_hashed_password VARCHAR(255);
    v_times_used INT;
    v_is_valid BOOLEAN;
    v_expiration TIMESTAMP;
    v_password_match BOOLEAN;
BEGIN
    -- Get token details
    SELECT st."CustomerID", c."Password", st."TimesUsed", st."IsValid", st."Expiration"
    INTO v_customer_id, v_hashed_password, v_times_used, v_is_valid, v_expiration
    FROM "SessionToken" st
    JOIN "Customers" c ON st."CustomerID" = c."CustomerID"
    WHERE st."Token" = p_token;

    -- Check if token exists
    IF v_customer_id IS NULL THEN
        RETURN QUERY SELECT false, NULL::INT, 'Invalid token';
        RETURN;
    END IF;

    -- Check if token is already expired
    IF v_is_valid THEN
        RETURN QUERY SELECT false, v_customer_id, 'Session expired. Please log in with your email and password.';
        RETURN;
    END IF;

    -- Check if token has expired by time
    IF CURRENT_TIMESTAMP > v_expiration THEN
        -- Mark token as expired
        UPDATE "SessionToken" 
        SET "IsValid" = false
        WHERE "Token" = p_token;
        
        RETURN QUERY SELECT false, v_customer_id, 'Session expired. Please log in with your email and password.';
        RETURN;
    END IF;

    -- Verify password
    SELECT (v_hashed_password = crypt(p_password, v_hashed_password)) INTO v_password_match;
    
    IF NOT v_password_match THEN
        RETURN QUERY SELECT false, v_customer_id, 'Invalid password';
        RETURN;
    END IF;

    -- Increment TimesUsed
    v_times_used := v_times_used + 1;
    
    -- Check if token has been used too many times
    IF v_times_used > 3 THEN
        -- Mark token as expired
        UPDATE "SessionToken" 
        SET 
            "TimesUsed" = v_times_used,
            "IsValid" = false
        WHERE "Token" = p_token;
        
        RETURN QUERY SELECT false, v_customer_id, 'Session limit reached. Please log in with your email and password.';
        RETURN;
    END IF;
    
    -- Update TimesUsed
    UPDATE "SessionToken" 
    SET "TimesUsed" = v_times_used
    WHERE "Token" = p_token;
    
    -- Return success
    RETURN QUERY SELECT true, v_customer_id, 'Login successful';
END;
$$ LANGUAGE plpgsql;

-- Function to create a new session token
CREATE OR REPLACE FUNCTION create_session_token(
    p_customer_id INT,
    p_token VARCHAR(500),
    p_expiration_hours INT DEFAULT 24
)
RETURNS VOID AS $$
BEGIN
    -- Expire all existing tokens for this user
    UPDATE "SessionToken"
    SET "IsValid" = false
    WHERE "CustomerID" = p_customer_id;
    
    -- Insert new token
    INSERT INTO "SessionToken" ("CustomerID", "Token", "Expiration")
    VALUES (p_customer_id, p_token, CURRENT_TIMESTAMP + (p_expiration_hours * INTERVAL '1 hour'));
END;
$$ LANGUAGE plpgsql;
