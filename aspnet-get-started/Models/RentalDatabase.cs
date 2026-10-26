using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace aspnet_get_started.Models
{
    public static class RentalDatabase
    {
        private static readonly Lazy<bool> Initialized = new Lazy<bool>(InitializeDatabase);

        private const string CreateSchema = @"
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        Phone nvarchar(30) NULL,
        Email nvarchar(255) NULL
    );
END;

IF OBJECT_ID(N'dbo.Properties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Properties
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        Address nvarchar(255) NOT NULL,
        MonthlyRent decimal(18,2) NOT NULL,
        TenantId int NULL,
        CONSTRAINT FK_Properties_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
    );
END;

IF OBJECT_ID(N'dbo.RentRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RentRecords
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PropertyId int NOT NULL,
        TenantId int NOT NULL,
        DueMonth date NOT NULL,
        DueDate date NOT NULL,
        RentAmount decimal(18,2) NOT NULL,
        AdvancePaid decimal(18,2) NOT NULL CONSTRAINT DF_RentRecords_AdvancePaid DEFAULT (0),
        CONSTRAINT UQ_RentRecords_Property_Month UNIQUE (PropertyId, DueMonth),
        CONSTRAINT FK_RentRecords_Properties FOREIGN KEY (PropertyId) REFERENCES dbo.Properties(Id),
        CONSTRAINT FK_RentRecords_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
    );
END;

IF COL_LENGTH(N'dbo.RentRecords', N'AdvancePaid') IS NULL
BEGIN
    ALTER TABLE dbo.RentRecords
    ADD AdvancePaid decimal(18,2) NOT NULL
        CONSTRAINT DF_RentRecords_AdvancePaid DEFAULT (0);
END;

IF OBJECT_ID(N'dbo.RentPayments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RentPayments
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RentRecordId int NOT NULL,
        Amount decimal(18,2) NOT NULL,
        PaidOn date NOT NULL,
        Note nvarchar(255) NULL,
        CONSTRAINT FK_RentPayments_RentRecords FOREIGN KEY (RentRecordId)
            REFERENCES dbo.RentRecords(Id) ON DELETE CASCADE
    );
END;";

        private static bool InitializeDatabase()
        {
            using (var connection = CreateConnection())
            using (var command = new SqlCommand(CreateSchema, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }

            return true;
        }

        private static SqlConnection CreateConnection()
        {
            var environmentConnectionString =
                Environment.GetEnvironmentVariable("SQLAZURECONNSTR_RentalDb")
                ?? Environment.GetEnvironmentVariable("SQLCONNSTR_RentalDb");
            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                return new SqlConnection(environmentConnectionString);
            }

            var setting = ConfigurationManager.ConnectionStrings["RentalDb"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new InvalidOperationException(
                    "The RentalDb connection string is missing. Configure it in Web.config or Azure App Service.");
            }

            return new SqlConnection(setting.ConnectionString);
        }

        private static SqlConnection OpenConnection()
        {
            if (!Initialized.Value)
            {
                throw new InvalidOperationException("The rental database could not be initialized.");
            }

            var connection = CreateConnection();
            connection.Open();
            return connection;
        }

        public static IList<Tenant> GetTenants()
        {
            var tenants = new List<Tenant>();
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(
                "SELECT Id, Name, Phone, Email FROM dbo.Tenants ORDER BY Name;", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tenants.Add(new Tenant
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
            }

            return tenants;
        }

        public static void SaveTenant(Tenant tenant)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(tenant.Id == 0
                ? "INSERT INTO dbo.Tenants (Name, Phone, Email) VALUES (@Name, @Phone, @Email);"
                : "UPDATE dbo.Tenants SET Name = @Name, Phone = @Phone, Email = @Email WHERE Id = @Id;", connection))
            {
                AddString(command, "@Name", 100, tenant.Name);
                AddString(command, "@Phone", 30, tenant.Phone);
                AddString(command, "@Email", 255, tenant.Email);
                if (tenant.Id != 0)
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = tenant.Id;
                }

                if (command.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The tenant could not be saved because it no longer exists.");
                }
            }
        }

        public static bool DeleteTenant(int id)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(
                @"DELETE FROM dbo.Tenants
                  WHERE Id = @Id
                    AND NOT EXISTS (SELECT 1 FROM dbo.Properties WHERE TenantId = @Id)
                    AND NOT EXISTS (SELECT 1 FROM dbo.RentRecords WHERE TenantId = @Id);", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                return command.ExecuteNonQuery() == 1;
            }
        }

        public static IList<RentalProperty> GetProperties()
        {
            var properties = new List<RentalProperty>();
            const string sql = @"
SELECT p.Id, p.Name, p.Address, p.MonthlyRent, p.TenantId, t.Name
FROM dbo.Properties p
LEFT JOIN dbo.Tenants t ON t.Id = p.TenantId
ORDER BY p.Name;";
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    properties.Add(new RentalProperty
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Address = reader.GetString(2),
                        MonthlyRent = reader.GetDecimal(3),
                        TenantId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                        TenantName = reader.IsDBNull(5) ? "Unassigned" : reader.GetString(5)
                    });
                }
            }

            return properties;
        }

        public static void SaveProperty(RentalProperty property)
        {
            const string insertSql = @"
INSERT INTO dbo.Properties (Name, Address, MonthlyRent, TenantId)
SELECT @Name, @Address, @MonthlyRent, @TenantId
WHERE @TenantId IS NULL OR EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantId);";
            const string updateSql = @"
UPDATE dbo.Properties
SET Name = @Name, Address = @Address, MonthlyRent = @MonthlyRent, TenantId = @TenantId
WHERE Id = @Id
  AND (@TenantId IS NULL OR EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantId));";
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(property.Id == 0 ? insertSql : updateSql, connection))
            {
                AddPropertyParameters(command, property);
                if (property.Id != 0)
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = property.Id;
                }

                if (command.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The property could not be saved because it no longer exists.");
                }
            }
        }

        public static bool DeleteProperty(int id)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(
                @"DELETE FROM dbo.Properties
                  WHERE Id = @Id
                    AND NOT EXISTS (SELECT 1 FROM dbo.RentRecords WHERE PropertyId = @Id);", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                return command.ExecuteNonQuery() == 1;
            }
        }

        public static bool SaveRentRecord(RentRecordForm form, DateTime dueMonth)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(form.Id == 0 ? InsertRentRecord : UpdateRentRecord, connection))
            {
                command.Parameters.Add("@PropertyId", SqlDbType.Int).Value = form.PropertyId;
                command.Parameters.Add("@DueMonth", SqlDbType.Date).Value = dueMonth;
                command.Parameters.Add("@DueDate", SqlDbType.Date).Value = form.DueDate.Date;
                command.Parameters.Add("@RentAmount", SqlDbType.Decimal).Value = form.RentAmount;
                command.Parameters["@RentAmount"].Precision = 18;
                command.Parameters["@RentAmount"].Scale = 2;
                command.Parameters.Add("@AdvancePaid", SqlDbType.Decimal).Value = form.AdvancePaid;
                command.Parameters["@AdvancePaid"].Precision = 18;
                command.Parameters["@AdvancePaid"].Scale = 2;
                if (form.Id != 0)
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = form.Id;
                }

                return command.ExecuteNonQuery() == 1;
            }
        }

        private const string InsertRentRecord = @"
INSERT INTO dbo.RentRecords (PropertyId, TenantId, DueMonth, DueDate, RentAmount, AdvancePaid)
SELECT Id, TenantId, @DueMonth, @DueDate, @RentAmount, @AdvancePaid
FROM dbo.Properties
WHERE Id = @PropertyId AND TenantId IS NOT NULL
  AND @AdvancePaid <= @RentAmount
  AND NOT EXISTS (SELECT 1 FROM dbo.RentRecords WHERE PropertyId = @PropertyId AND DueMonth = @DueMonth);";

        private const string UpdateRentRecord = @"
UPDATE r
SET PropertyId = p.Id, TenantId = p.TenantId, DueMonth = @DueMonth,
    DueDate = @DueDate, RentAmount = @RentAmount, AdvancePaid = @AdvancePaid
FROM dbo.RentRecords r
INNER JOIN dbo.Properties p ON p.Id = @PropertyId AND p.TenantId IS NOT NULL
WHERE r.Id = @Id
  AND @RentAmount >= @AdvancePaid + (SELECT ISNULL(SUM(Amount), 0) FROM dbo.RentPayments WHERE RentRecordId = r.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.RentRecords other
                  WHERE other.PropertyId = @PropertyId AND other.DueMonth = @DueMonth AND other.Id <> r.Id);";

        public static IList<RentalProperty> GetAssignableProperties()
        {
            var properties = new List<RentalProperty>();
            const string sql = @"
SELECT p.Id, p.Name, p.MonthlyRent, t.Name
FROM dbo.Properties p
INNER JOIN dbo.Tenants t ON t.Id = p.TenantId
ORDER BY p.Name;";
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    properties.Add(new RentalProperty
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        MonthlyRent = reader.GetDecimal(2),
                        TenantName = reader.GetString(3)
                    });
                }
            }

            return properties;
        }

        public static RentalRecord GetRentRecord(int id)
        {
            const string sql = @"
SELECT r.Id, r.PropertyId, r.TenantId, t.Name, p.Name, r.DueMonth, r.DueDate,
       r.RentAmount, r.AdvancePaid, ISNULL(SUM(pay.Amount), 0) AS AmountPaid
FROM dbo.RentRecords r
INNER JOIN dbo.Tenants t ON t.Id = r.TenantId
INNER JOIN dbo.Properties p ON p.Id = r.PropertyId
LEFT JOIN dbo.RentPayments pay ON pay.RentRecordId = r.Id
WHERE r.Id = @Id
GROUP BY r.Id, r.PropertyId, r.TenantId, t.Name, p.Name, r.DueMonth,
         r.DueDate, r.RentAmount, r.AdvancePaid;";
            RentalRecord record = null;
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        record = ReadRecord(reader);
                    }
                }
            }

            if (record != null)
            {
                record.Payments = GetPayments(id);
            }

            return record;
        }

        public static RentalDashboard GetDashboard(string search, string status)
        {
            const string totalsSql = @"
SELECT COUNT(*), ISNULL(SUM(DueAmount), 0), ISNULL(SUM(AdvancePaid), 0), ISNULL(SUM(AmountPaid), 0)
FROM
(
    SELECT r.RentAmount,
           r.AdvancePaid,
           ISNULL(SUM(pay.Amount), 0) AS AmountPaid,
           CASE WHEN r.RentAmount - r.AdvancePaid - ISNULL(SUM(pay.Amount), 0) < 0
                THEN 0 ELSE r.RentAmount - r.AdvancePaid - ISNULL(SUM(pay.Amount), 0) END AS DueAmount
    FROM dbo.RentRecords r
    LEFT JOIN dbo.RentPayments pay ON pay.RentRecordId = r.Id
    GROUP BY r.Id, r.RentAmount, r.AdvancePaid
) charges;";
            const string sql = @"
SELECT r.Id, r.PropertyId, r.TenantId, t.Name, p.Name, r.DueMonth, r.DueDate,
       r.RentAmount, r.AdvancePaid, ISNULL(SUM(pay.Amount), 0) AS AmountPaid
FROM dbo.RentRecords r
INNER JOIN dbo.Tenants t ON t.Id = r.TenantId
INNER JOIN dbo.Properties p ON p.Id = r.PropertyId
LEFT JOIN dbo.RentPayments pay ON pay.RentRecordId = r.Id
WHERE (@Search = N'' OR t.Name LIKE N'%' + @Search + N'%'
       OR p.Name LIKE N'%' + @Search + N'%'
       OR CONVERT(nvarchar(7), r.DueMonth, 126) LIKE N'%' + @Search + N'%')
GROUP BY r.Id, r.PropertyId, r.TenantId, t.Name, p.Name, r.DueMonth,
         r.DueDate, r.RentAmount, r.AdvancePaid
HAVING (@Status = N'' OR @Status = CASE
           WHEN r.RentAmount - r.AdvancePaid - ISNULL(SUM(pay.Amount), 0) <= 0 THEN N'Paid'
           WHEN r.AdvancePaid > 0 OR ISNULL(SUM(pay.Amount), 0) > 0 THEN N'Partial'
           WHEN r.DueDate < CAST(GETDATE() AS date) THEN N'Overdue'
           ELSE N'Pending' END)
ORDER BY r.DueMonth DESC, t.Name, p.Name;";
            var dashboard = new RentalDashboard
            {
                Search = search,
                Status = status
            };
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                using (var totalsCommand = new SqlCommand(totalsSql, connection))
                using (var totalsReader = totalsCommand.ExecuteReader())
                {
                    if (totalsReader.Read())
                    {
                        dashboard.TotalRecords = totalsReader.GetInt32(0);
                        dashboard.TotalDue = totalsReader.GetDecimal(1);
                        dashboard.TotalAdvance = totalsReader.GetDecimal(2);
                        dashboard.TotalCollected = totalsReader.GetDecimal(3);
                    }
                }

                AddString(command, "@Search", 255, search ?? string.Empty);
                AddString(command, "@Status", 20, status ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = ReadRecord(reader);
                        dashboard.Records.Add(record);
                    }
                }
            }

            return dashboard;
        }

        public static IList<RentPayment> GetPayments(int rentRecordId)
        {
            var payments = new List<RentPayment>();
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(
                @"SELECT Id, RentRecordId, Amount, PaidOn, Note
                  FROM dbo.RentPayments WHERE RentRecordId = @RentRecordId
                  ORDER BY PaidOn DESC, Id DESC;", connection))
            {
                command.Parameters.Add("@RentRecordId", SqlDbType.Int).Value = rentRecordId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payments.Add(new RentPayment
                        {
                            Id = reader.GetInt32(0),
                            RentRecordId = reader.GetInt32(1),
                            Amount = reader.GetDecimal(2),
                            PaidOn = reader.GetDateTime(3),
                            Note = reader.IsDBNull(4) ? null : reader.GetString(4)
                        });
                    }
                }
            }

            return payments;
        }

        public static bool AddPayment(int rentRecordId, RentPayment payment)
        {
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                const string dueSql = @"
SELECT r.RentAmount - r.AdvancePaid - ISNULL(SUM(p.Amount), 0)
FROM dbo.RentRecords r WITH (UPDLOCK, HOLDLOCK)
LEFT JOIN dbo.RentPayments p ON p.RentRecordId = r.Id
WHERE r.Id = @Id
GROUP BY r.RentAmount, r.AdvancePaid;";
                decimal dueAmount;
                using (var command = new SqlCommand(dueSql, connection, transaction))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = rentRecordId;
                    var result = command.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    dueAmount = Convert.ToDecimal(result);
                }

                if (payment.Amount <= 0 || payment.Amount > dueAmount)
                {
                    transaction.Rollback();
                    return false;
                }

                const string insertSql = @"
INSERT INTO dbo.RentPayments (RentRecordId, Amount, PaidOn, Note)
VALUES (@RentRecordId, @Amount, @PaidOn, @Note);";
                using (var command = new SqlCommand(insertSql, connection, transaction))
                {
                    command.Parameters.Add("@RentRecordId", SqlDbType.Int).Value = rentRecordId;
                    var amount = command.Parameters.Add("@Amount", SqlDbType.Decimal);
                    amount.Precision = 18;
                    amount.Scale = 2;
                    amount.Value = payment.Amount;
                    command.Parameters.Add("@PaidOn", SqlDbType.Date).Value = payment.PaidOn.Date;
                    AddString(command, "@Note", 255, payment.Note);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
        }

        public static bool DeletePayment(int rentRecordId, int paymentId)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand(
                "DELETE FROM dbo.RentPayments WHERE Id = @Id AND RentRecordId = @RentRecordId;", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = paymentId;
                command.Parameters.Add("@RentRecordId", SqlDbType.Int).Value = rentRecordId;
                return command.ExecuteNonQuery() == 1;
            }
        }

        public static bool DeleteRentRecord(int id)
        {
            using (var connection = OpenConnection())
            using (var command = new SqlCommand("DELETE FROM dbo.RentRecords WHERE Id = @Id;", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                return command.ExecuteNonQuery() == 1;
            }
        }

        private static RentalRecord ReadRecord(SqlDataReader reader)
        {
            var rentAmount = reader.GetDecimal(7);
            var advancePaid = reader.GetDecimal(8);
            var amountPaid = reader.GetDecimal(9);
            var dueAmount = Math.Max(0m, rentAmount - advancePaid - amountPaid);
            var dueDate = reader.GetDateTime(6);

            return new RentalRecord
            {
                Id = reader.GetInt32(0),
                PropertyId = reader.GetInt32(1),
                TenantId = reader.GetInt32(2),
                TenantName = reader.GetString(3),
                PropertyName = reader.GetString(4),
                DueMonth = reader.GetDateTime(5),
                DueDate = dueDate,
                RentAmount = rentAmount,
                AdvancePaid = advancePaid,
                AmountPaid = amountPaid,
                DueAmount = dueAmount,
                Status = dueAmount == 0m ? "Paid"
                    : advancePaid + amountPaid > 0m ? "Partial"
                    : dueDate.Date < DateTime.Today ? "Overdue" : "Pending"
            };
        }

        private static void AddPropertyParameters(SqlCommand command, RentalProperty property)
        {
            AddString(command, "@Name", 100, property.Name);
            AddString(command, "@Address", 255, property.Address);
            var monthlyRent = command.Parameters.Add("@MonthlyRent", SqlDbType.Decimal);
            monthlyRent.Precision = 18;
            monthlyRent.Scale = 2;
            monthlyRent.Value = property.MonthlyRent;
            command.Parameters.Add("@TenantId", SqlDbType.Int).Value =
                property.TenantId.HasValue ? (object)property.TenantId.Value : DBNull.Value;
        }

        private static void AddString(SqlCommand command, string name, int size, string value)
        {
            command.Parameters.Add(name, SqlDbType.NVarChar, size).Value =
                string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
