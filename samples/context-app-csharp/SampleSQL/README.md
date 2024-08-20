
 
if set up script was okay
 ```bash
 Starting setup script
Changed database context to 'master'.
2024-08-20 21:03:22.48 spid51      [5]. Feature Status: PVS: 0. CTR: 0. ConcurrentPFSUpdate: 1.
2024-08-20 21:03:22.49 spid51      Starting up database 'MySampleDB'.
2024-08-20 21:03:22.51 spid51      Parallel redo is started for database 'MySampleDB' with worker pool size [10].
2024-08-20 21:03:22.53 spid51      Parallel redo is shutdown for database 'MySampleDB' with worker pool size [10].
Created MySampleDB database
Changed database context to 'MySampleDB'.
Switched to MySampleDB database

(4 rows affected)
Created and populated CountryMeasurements table
Changed database context to 'master'.
Switched to master database
Attempting to create or modify login for sampleuser,APP_PASSWORD
Created login for sampleuser,APP_PASSWORD with default database MySampleDB
Granting CONNECT SQL to sampleuser,APP_PASSWORD
Changed database context to 'MySampleDB'.
Switched to MySampleDB database for user creation/modification
Attempting to create or modify user for sampleuser,APP_PASSWORD
Created user for sampleuser,APP_PASSWORD and added to db_owner role
Verifying sampleuser,APP_PASSWORD permissions:
Owner Object Grantee                                        Grantor ProtectType Action         Column
----- ------ ---------------------------------------------- ------- ----------- -------------- ------
.     .      sampleuser,APP_PASSWORD                        dbo     Grant       CONNECT        .

(1 rows affected)
Setup script completed
```
Check logs for database, table and data creation
```bash
kubectl logs $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}")
```
Logs will look
```
Starting setup script
Changed database context to 'master'.
2024-08-20 22:07:53.01 spid51      [5]. Feature Status: PVS: 0. CTR: 0. ConcurrentPFSUpdate: 1.
2024-08-20 22:07:53.01 spid51      Starting up database 'MySampleDB'.
2024-08-20 22:07:53.04 spid51      Parallel redo is started for database 'MySampleDB' with worker pool size [10].
2024-08-20 22:07:53.06 spid51      Parallel redo is shutdown for database 'MySampleDB' with worker pool size [10].
Created MySampleDB database
Changed database context to 'MySampleDB'.
Switched to MySampleDB database

(4 rows affected)
Created and populated CountryMeasurements table
Setup script completed
```
Set SA_PASSWORD ENV var before verications
```bash
SA_PASSWORD=YourStrongPassword123!
```
Verifly that sa can log in. This will open a SQL command prompt). Type `QUIT` to exit the prompt.
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P $SA_PASSWORD -C
```

Verify database creation
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT name FROM sys.databases WHERE name = 'MySampleDB'" -C
name
--------------------------------------------------------------------------------------------------------------------------------
MySampleDB

(1 rows affected)
```

Verify table creation
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -d MySampleDB -Q "SELECT name FROM sys.tables WHERE name = 'CountryMeasurements'" -C
name
--------------------------------------------------------------------------------------------------------------------------------
CountryMeasurements

(1 rows affected)
```

Verify data in the table
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -d MySampleDB -Q "SELECT * FROM CountryMeasurements" -C

ID          Country Viscosity Sweetness ParticleSize Overall
----------- ------- --------- --------- ------------ -------
          1 us            .50       .80          .70     .40
          2 fr            .60       .85          .75     .45
          3 jp            .53       .83          .73     .43
          4 uk            .51       .81          .71     .41

(4 rows affected)
```
verify permsissions for "sa" user
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT permission_name FROM fn_my_permissions(NULL, 'SERVER') WHERE permission_name = 'ALTER ANY LOGIN'" -C

permission_name
------------------------------------------------------------
ALTER ANY LOGIN

(1 rows affected)
```

Check if there are any policy violations preventing login creation
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT name, is_policy_checked, is_expiration_checked FROM sys.sql_logins WHERE name = 'sa'" -C
name                                                                                                                             is_policy_checked is_expiration_checked
-------------------------------------------------------------------------------------------------------------------------------- ----------------- ---------------------
sa                                                                                                                                               1                     0

(1 rows affected)
```
Before next steps creat a password for the app user and set it as an ENV var in the current shell session
```bash
APP_PASSWORD=NewComplexP@ssw0rd123$
```

Create user on server
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "CREATE LOGIN [sampleuser] WITH PASSWORD = N'$APP_PASSWORD'; SELECT name FROM sys.server_principals WHERE name = 'sampleuser';" -C

name
--------------------------------------------------------------------------------------------------------------------------------
sampleuser

(1 rows affected)
```
First, let's check the properties of the 'sampleuser' login from server principals:
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT name, type_desc, is_disabled FROM sys.server_principals WHERE name = 'sampleuser';" -C

name                                                                                                                             type_desc                                                    is_disabled
-------------------------------------------------------------------------------------------------------------------------------- ------------------------------------------------------------ -----------
sampleuser                                                                                                                       SQL_LOGIN                                                              0

(1 rows affected)
```

Create user on database
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "USE MySampleDB; CREATE USER [sampleuser] FOR LOGIN [sampleuser]; ALTER ROLE db_owner ADD MEMBER [sampleuser];" -C
```
Now, let's check if the user exists in the 'MySampleDB' database:
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "USE MySampleDB; SELECT name, type_desc FROM sys.database_principals WHERE name = 'sampleuser';" -C

Changed database context to 'MySampleDB'.
name                                                                                                                             type_desc
-------------------------------------------------------------------------------------------------------------------------------- ------------------------------------------------------------
sampleuser                                                                                                                       SQL_USER

(1 rows affected)
```

Let's verify the roles assigned to 'sampleuser' in 'MySampleDB':
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "USE MySampleDB; SELECT r.name AS RoleName FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id WHERE m.name = 'sampleuser';" -C

Changed database context to 'MySampleDB'.
RoleName
--------------------------------------------------------------------------------------------------------------------------------
db_owner

(1 rows affected)
```

Finally, let's try to connect to the database using the 'sampleuser' login:
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sampleuser -P $APP_PASSWORD -Q "SELECT DB_NAME();" -C
```
After running these commands, the 'sampleuser' should be able to log in to the MySampleDB database. To test this, you can try:
```bash
kubectl exec -it $(kubectl get pods -l app=mssql -o jsonpath="{.items[0].metadata.name}") -- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sampleuser -P $APP_PASSWORD -d MySampleDB -Q "SELECT DB_NAME() AS [Current Database];" -C

Current Database
--------------------------------------------------------------------------------------------------------------------------------
MySampleDB

(1 rows affected)
```