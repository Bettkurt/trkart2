-- Quick fix for TopUp ExternalRef constraint issue
-- This removes the unique constraint temporarily to allow TopUp transactions

-- Check what duplicate ExternalRefs exist
SELECT "ExternalRef", COUNT(*) as count 
FROM "Transaction" 
WHERE "ExternalRef" IS NOT NULL 
GROUP BY "ExternalRef" 
HAVING COUNT(*) > 1;

-- Show recent transactions with ExternalRef
SELECT "TransactionID", "ExternalRef", "TransactionType", "TransactionDate", "TransactionStatus"
FROM "Transaction" 
WHERE "ExternalRef" IS NOT NULL 
ORDER BY "TransactionDate" DESC 
LIMIT 10;

-- Drop the problematic unique index (temporary fix)
DROP INDEX IF EXISTS "IX_Transaction_ExternalRef";

-- Create a non-unique index instead for performance (optional)
CREATE INDEX IF NOT EXISTS "IDX_Transaction_ExternalRef_NonUnique" 
ON "Transaction" ("ExternalRef") 
WHERE "ExternalRef" IS NOT NULL;

-- Print confirmation
SELECT 'TopUp constraint fix applied - ExternalRef is no longer unique' as status;
