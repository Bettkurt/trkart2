-------------------------------------------------------------------------------------------
-------------------------------------CardLimits--------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "CardLimits" (
    "LimitID" SERIAL PRIMARY KEY,
    "CardID" INT NOT NULL,
    -- Defaulted 10k. Lowest tier, standart card has 20k limit. 
    -- So, if we see 10k limit, we know something with CardType went wrong
    "PayLimit" DECIMAL(10, 2) NOT NULL DEFAULT 10000.00,
    "PayMaxLimit" DECIMAL(10, 2) NOT NULL DEFAULT 10000.00,
    "PayLimitUpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "TransferLimit" DECIMAL(10, 2) NOT NULL DEFAULT 10000.00,
    "TransferMaxLimit" DECIMAL(10, 2) NOT NULL DEFAULT 10000.00,
    "TransferLimitUpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "FK_CardLimits_UserCard_CardID"
        FOREIGN KEY ("CardID") 
        REFERENCES "UserCard"("CardID") 
        ON DELETE CASCADE
);

-----------------------------Create Card Limits for a New Card-----------------------------

-- Function to create card limits when a new card is added
CREATE OR REPLACE FUNCTION create_card_limits_trigger_fn()
RETURNS TRIGGER AS $$
BEGIN
    -- Insert default limits based on card type
    -- Adjust the values as needed for each card type
    INSERT INTO "CardLimits" (
        "CardID",
        "PayLimit",
        "PayMaxLimit",
        "PayLimitUpdatedAt",
        "TransferLimit",
        "TransferMaxLimit",
        "TransferLimitUpdatedAt"
    ) VALUES (
        NEW."CardID",

        CASE -- PayLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END, 
        CASE -- PayMaxLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END,
        -- PayLimitUpdatedAt
        CURRENT_TIMESTAMP,
        
        CASE -- TransferLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END, 
        CASE -- TransferMaxLimit
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE 10000.00 -- Default for any other types
        END, 
        -- TransferLimitUpdatedAt
        CURRENT_TIMESTAMP  
    );
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE TRIGGER trg_create_card_limits_for_new_card
AFTER INSERT ON "UserCard"
FOR EACH ROW
EXECUTE FUNCTION create_card_limits_trigger_fn();

-----------------------------Handle Card Type Change for Limits-----------------------------

-- Function to handle card type changes
CREATE OR REPLACE FUNCTION handle_card_type_change_for_limits()
RETURNS TRIGGER AS $$
DECLARE
    v_old_max_pay DECIMAL(10,2);
    v_old_max_transfer DECIMAL(10,2);
    v_new_max_pay DECIMAL(10,2);
    v_new_max_transfer DECIMAL(10,2);
BEGIN
    -- Only proceed if CardType actually changed
    IF NEW."CardType" = OLD."CardType" THEN
        RETURN NEW;
    END IF;

    -- Get current max limits from CardLimits
    SELECT 
        "PayMaxLimit", 
        "TransferMaxLimit"
    INTO 
        v_old_max_pay, 
        v_old_max_transfer
    FROM "CardLimits"
    WHERE "CardID" = NEW."CardID";

    -- Calculate new max limits based on new card type
    SELECT 
        CASE 
            WHEN NEW."CardType" = 0 THEN 20000.00  -- Standard
            WHEN NEW."CardType" = 1 THEN 50000.00  -- Gold
            WHEN NEW."CardType" = 2 THEN 100000.00 -- Platinum
            ELSE v_old_max_pay
        END,
        CASE 
            WHEN NEW."CardType" = 0 THEN 20000.00
            WHEN NEW."CardType" = 1 THEN 50000.00
            WHEN NEW."CardType" = 2 THEN 100000.00
            ELSE v_old_max_transfer
        END
    INTO 
        v_new_max_pay, 
        v_new_max_transfer;

    -- Determine if this is an upgrade or downgrade
    -- If new max limits are lower than old ones, it's a downgrade
    IF v_new_max_pay < v_old_max_pay OR v_new_max_transfer < v_old_max_transfer THEN
        -- Downgrade: Enforce new max limits on current limits
        UPDATE "CardLimits"
        SET 
            -- Since this is a downgare in card type/tier; 
            -- Choose the least of the current pay limit or the card's new type max pay limit
            "PayLimit" = LEAST("PayLimit", v_new_max_pay),
            "PayMaxLimit" = v_new_max_pay,
            "PayLimitUpdatedAt" = CURRENT_TIMESTAMP,
            -- Since this is a downgare in card type/tier; 
            -- Choose the least of the current transfer limit or the card's new type max transfer limit
            "TransferLimit" = LEAST("TransferLimit", v_new_max_transfer),
            "TransferMaxLimit" = v_new_max_transfer,
            "TransferLimitUpdatedAt" = CURRENT_TIMESTAMP
        WHERE "CardID" = NEW."CardID";
    ELSE
        -- Upgrade: Only update max limits, keep current limits
        UPDATE "CardLimits"
        SET 
            "PayMaxLimit" = v_new_max_pay,
            "TransferMaxLimit" = v_new_max_transfer
        WHERE "CardID" = NEW."CardID";
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger
CREATE OR REPLACE TRIGGER trg_after_update_usercard_type
AFTER UPDATE OF "CardType" ON "UserCard"
FOR EACH ROW
WHEN (NEW."CardType" IS DISTINCT FROM OLD."CardType")
EXECUTE FUNCTION handle_card_type_change_for_limits();