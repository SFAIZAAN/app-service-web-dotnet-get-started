# SF Rental

SF Rental is an ASP.NET MVC 5 application targeting .NET Framework 4.8. It
tracks tenants, properties, monthly rent charges, and payments using SQL Server
or Azure SQL. The dashboard tracks advances, payments, outstanding balances,
and overdue rent.

## Database setup

The application creates its tables the first time it connects. The configured
database must already exist, and its login must be allowed to create tables and
constraints.

For local development, create a SQL Server LocalDB database named `SFRentals`
and run the app with IIS Express. With SQL Server LocalDB installed, create it
once with:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE SFRentals"
```

The default connection string is in `aspnet-get-started/Web.config`.

For Azure App Service:

1. Create an Azure SQL database and allow the App Service to connect to its SQL
   server.
2. In **App Service → Settings → Environment variables → Connection strings**,
   add a connection string named `RentalDb` with type **SQLAzure**.
3. Save the setting and restart the App Service. Do not put database passwords
   in source control.

## CI with Azure Pipelines

The pipeline restores NuGet packages, builds the Release configuration, and
publishes a Web Deploy package on pushes and pull requests targeting `main`.
Download the `drop` artifact from a successful run to retrieve the package.

This pipeline does not deploy to Azure. To enable deployment later, create a
Windows App Service and an Azure Resource Manager service connection in Azure
DevOps. A GitHub service connection can check out source but cannot authorize
deployment to Azure. Configure the App Service's `RentalDb` connection string
separately as described above.

## Run with Docker

This application uses .NET Framework 4.8 and IIS, so Docker must be in Windows
container mode. The container needs access to a SQL Server database; LocalDB is
not included in the container. Pass an Azure SQL or other SQL Server connection
string through an environment variable:

```powershell
docker build -t sf-rental .
docker run --rm -d --name sf-rental-app -p 8080:80 `
  -e SQLAZURECONNSTR_RentalDb="<your SQL Server connection string>" `
  sf-rental
```

Open [http://localhost:8080](http://localhost:8080). To stop the container:

```powershell
docker stop sf-rental-app
```

The app currently has no user sign-in or authorization. Add authentication
before exposing tenant or payment information to the public internet.
