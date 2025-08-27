#!/bin/bash

# Quick fix script for TopUp ExternalRef constraint issue
echo "🔧 Applying TopUp constraint fix..."

# Database connection details
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="TRKart"
DB_USER="postgres"
DB_PASSWORD="1234"

# Path to fix SQL file
FIX_FILE="fix_topup_constraint.sql"

# Check if fix file exists
if [ ! -f "$FIX_FILE" ]; then
    echo "❌ Error: Fix file not found at $FIX_FILE"
    exit 1
fi

# Try different PostgreSQL client locations
PSQL_CMD=""
if command -v psql &> /dev/null; then
    PSQL_CMD="psql"
elif [ -f "/Applications/PostgreSQL 17/Contents/Versions/17/bin/psql" ]; then
    PSQL_CMD="/Applications/PostgreSQL 17/Contents/Versions/17/bin/psql"
else
    echo "❌ Error: psql command not found."
    echo ""
    echo "Manual fix instructions:"
    echo "1. Connect to your PostgreSQL database"
    echo "2. Run: DROP INDEX IF EXISTS \"IX_Transaction_ExternalRef\";"
    echo "3. This will remove the unique constraint causing the error"
    echo ""
    echo "Database connection details:"
    echo "Host: $DB_HOST"
    echo "Port: $DB_PORT"
    echo "Database: $DB_NAME"
    echo "Username: $DB_USER"
    exit 1
fi

# Set PGPASSWORD environment variable
export PGPASSWORD="$DB_PASSWORD"

# Run the fix
echo "🔧 Removing ExternalRef unique constraint..."
$PSQL_CMD -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$FIX_FILE"

if [ $? -eq 0 ]; then
    echo "✅ TopUp constraint fix completed successfully!"
    echo ""
    echo "🎯 You can now test TopUp functionality:"
    echo "- ExternalRef duplicates are now allowed"
    echo "- TopUp transactions should work without constraint errors"
    echo "- The unique constraint can be re-added later if needed"
else
    echo "❌ Fix failed. Please run the manual commands."
fi

# Unset password for security
unset PGPASSWORD

echo "🔧 Fix script completed."
