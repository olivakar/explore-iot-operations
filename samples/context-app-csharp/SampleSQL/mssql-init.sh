#!/bin/bash

# Start SQL Server
/opt/mssql/bin/sqlservr &

# Wait for SQL Server to start up
while ! /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT 1" &> /dev/null
do
    echo "Waiting for SQL Server to start up..."
    sleep 5
done

echo "SQL Server started"

# Function to run setup script
run_setup_script() {
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -d master -i /usr/config/setup.sql
}

# Keep trying to run the setup script until it succeeds
until run_setup_script
do
    echo "Setup script failed, retrying in 5 seconds..."
    sleep 5
done

echo "Setup script completed successfully"

# Keep the container running
wait