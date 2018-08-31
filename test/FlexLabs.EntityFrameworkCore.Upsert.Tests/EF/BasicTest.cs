﻿//#define USE_POSTGRESQL
//#define USE_SQLSERVER
//#define USE_MYSQL
#define USE_SQLITE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF.Base;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF
{
    public class BasicTest : IClassFixture<BasicTest.Contexts>
    {
        public class Contexts : IDisposable
        {
            private const string Postgres_ImageName = "flexlabs_upsert_test_postgres";
            private const string Postgres_Port = "25432";
            private static readonly string Postgres_Connection = $"Server=localhost;Port={Postgres_Port};Database={Username};Username={Username};Password={Password}";

            private const string SqlServer_ImageName = "flexlabs_upsert_test_sqlserver";
            private const string SqlServer_Port = "21433";
            private static readonly string SqlServer_Connection = $"Server=localhost,{SqlServer_Port};Database={Username};User Id=sa;Password={Password}";

            private const string MySql_ImageName = "flexlabs_upsert_test_mysql";
            private const string MySql_Port = "23306";
            private static readonly string MySql_Connection = $"Server=localhost;Port={MySql_Port};Database={Username};Uid=root;Pwd={Password}";

            private static readonly string Sqlite_Connection = $"Data Source={Username}.db";

            private const string Username = "testuser";
            private const string Password = "Password12!";

            private static readonly string AppVeyor_Postgres_Connection = $"Server=localhost;Port=5432;Database={Username};Username=postgres;Password={Password}";
            private static readonly string AppVeyor_SqlServer_Connection = $"Server=(local)\\SQL2017;Database={Username};User Id=sa;Password={Password}";
            private static readonly string AppVeyor_MySql_Connection = $"Server=localhost;Port=3306;Database={Username};Uid=root;Pwd={Password}";
            private static readonly string AppVeyor_Sqlite_Connection = $"Data Source={Username}.db";

            private bool IsAppVeyor => Environment.GetEnvironmentVariable("APPVEYOR") != null;

            private IDictionary<TestDbContext.DbDriver, Process> _processes;
            public IDictionary<TestDbContext.DbDriver, DbContextOptions<TestDbContext>> _dataContexts;

            public Contexts()
            {
                _processes = new Dictionary<TestDbContext.DbDriver, Process>();
                _dataContexts = new Dictionary<TestDbContext.DbDriver, DbContextOptions<TestDbContext>>();

                if (IsAppVeyor)
                {
                    WaitForConnection(TestDbContext.DbDriver.Postgres, AppVeyor_Postgres_Connection);
                    WaitForConnection(TestDbContext.DbDriver.MSSQL, AppVeyor_SqlServer_Connection);
                    WaitForConnection(TestDbContext.DbDriver.MySQL, AppVeyor_MySql_Connection);
                    WaitForConnection(TestDbContext.DbDriver.Sqlite, AppVeyor_Sqlite_Connection);
                }
                else
                {
#if USE_POSTGRESQL
                    _processes[TestDbContext.DbDriver.Postgres] = Process.Start("docker",
                        $"run --name {Postgres_ImageName} --platform linux -e POSTGRES_USER={Username} -e POSTGRES_PASSWORD={Password} -e POSTGRES_DB={Username} -p {Postgres_Port}:5432 postgres:alpine");
                    WaitForConnection(TestDbContext.DbDriver.Postgres, Postgres_Connection);
#endif
#if USE_SQLSERVER
                    _processes[TestDbContext.DbDriver.MSSQL] = Process.Start("docker",
                        $"run --name {SqlServer_ImageName} --platform linux -e ACCEPT_EULA=Y -e MSSQL_PID=Express -e SA_PASSWORD={Password} -p {SqlServer_Port}:1433 microsoft/mssql-server-linux");
                    WaitForConnection(TestDbContext.DbDriver.MSSQL, SqlServer_Connection);
#endif
#if USE_MYSQL
                    _processes[TestDbContext.DbDriver.MySQL] = Process.Start("docker",
                        $"run --name {MySql_ImageName} --platform linux -e MYSQL_ROOT_PASSWORD={Password} -e MYSQL_USER={Username} -e MYSQL_PASSWORD={Password} -e MYSQL_DATABASE={Username} -p {MySql_Port}:3306 mysql");
                    WaitForConnection(TestDbContext.DbDriver.MySQL, MySql_Connection);
#endif

#if USE_SQLITE
                    WaitForConnection(TestDbContext.DbDriver.Sqlite, Sqlite_Connection);
#endif
                }
            }

            private void WaitForConnection(TestDbContext.DbDriver driver, string connectionString)
            {
                var options = TestDbContext.Configure(connectionString, driver);
                var startTime = DateTime.Now;
                while (DateTime.Now.Subtract(startTime) < TimeSpan.FromSeconds(200))
                {
                    bool isSuccess = false;
                    TestDbContext context = null;
                    Console.WriteLine("Connecting to " + driver);
                    try
                    {
                        context = new TestDbContext(options);
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                        _dataContexts[driver] = options;
                        isSuccess = true;
                        Console.WriteLine(" - Connection Successful!");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(" - EXCEPTION: " + ex.Message);
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    finally
                    {
                        if (!isSuccess)
                            context?.Dispose();
                    }
                }
            }

            public void Dispose()
            {
                foreach (var context in _processes.Values)
                    context.Dispose();

                if (!IsAppVeyor)
                {
                    //using (var processRm = Process.Start("docker", $"rm -f {Postgres_ImageName}"))
                    //{
                    //    processRm.WaitForExit();
                    //}
                    //using (var processRm = Process.Start("docker", $"rm -f {SqlServer_ImageName}"))
                    //{
                    //    processRm.WaitForExit();
                    //}
                    //using (var processRm = Process.Start("docker", $"rm -f {MySql_ImageName}"))
                    //{
                    //    processRm.WaitForExit();
                    //}
                }
            }
        }

        private IDictionary<TestDbContext.DbDriver, DbContextOptions<TestDbContext>> _dataContexts;
        Country _dbCountry = new Country
        {
            Name = "...loading...",
            ISO = "AU",
            Created = new DateTime(1970, 1, 1),
            Updated = new DateTime(1970, 1, 1),
        };
        PageVisit _dbVisit = new PageVisit
        {
            UserID = 1,
            Date = DateTime.Today,
            Visits = 5,
            FirstVisit = new DateTime(1970, 1, 1),
            LastVisit = new DateTime(1970, 1, 1),
        };
        DateTime _now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        public BasicTest(Contexts contexts)
        {
            _dataContexts = contexts._dataContexts;
        }

        private void ResetDb(TestDbContext.DbDriver driver)
        {
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                dbContext.SaveChanges();
            }
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                //dbContext.Countries.RemoveRange(dbContext.Countries);
                //dbContext.DashTable.RemoveRange(dbContext.DashTable);
                //dbContext.SchemaTable.RemoveRange(dbContext.SchemaTable);
                //dbContext.PageVisits.RemoveRange(dbContext.PageVisits);

                dbContext.Countries.Add(_dbCountry);
                dbContext.PageVisits.Add(_dbVisit);
                dbContext.SaveChanges();
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_InitialDbState(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                Assert.Empty(dbContext.SchemaTable);
                Assert.Empty(dbContext.DashTable);
                Assert.Collection(dbContext.Countries, c => Assert.Equal("AU", c.ISO));
                Assert.Collection(dbContext.PageVisits, c => Assert.Equal(1, c.UserID));
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_Country_Update_NoColumns(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                var newCountry = new Country
                {
                    Name = "Australia",
                    ISO = "AU",
                    Created = _now,
                    Updated = _now,
                };

                dbContext.Upsert(newCountry)
                    .On(c => c.ISO)
                    .Run();

                var country = dbContext.Countries.Single(c => c.ISO == newCountry.ISO);
                Assert.NotNull(country);
                Assert.Equal(newCountry.Name, country.Name);
                Assert.Equal(newCountry.Created, country.Created);
                Assert.Equal(newCountry.Updated, country.Updated);
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_Country_Update_SelectedColumns(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                var newCountry = new Country
                {
                    Name = "Australia",
                    ISO = "AU",
                    Created = _now,
                    Updated = _now,
                };

                dbContext.Upsert(newCountry)
                    .On(c => c.ISO)
                    .UpdateColumns(c => new Country
                    {
                        Name = newCountry.Name,
                        Updated = newCountry.Updated,
                    })
                    .Run();

                var country = dbContext.Countries.Single(c => c.ISO == newCountry.ISO);
                Assert.NotNull(country);
                Assert.Equal(newCountry.Name, country.Name);
                Assert.NotEqual(newCountry.Created, country.Created);
                Assert.Equal(_dbCountry.Created, country.Created);
                Assert.Equal(newCountry.Updated, country.Updated);
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_Country_Insert(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                var newCountry = new Country
                {
                    Name = "United Kingdon",
                    ISO = "GB",
                    Created = _now,
                    Updated = _now,
                };

                dbContext.Upsert(newCountry)
                    .On(c => c.ISO)
                    .UpdateColumns(c => new Country
                    {
                        Name = newCountry.Name,
                        Updated = newCountry.Updated,
                    })
                    .Run();

                var country = dbContext.Countries.Single(c => c.ISO == newCountry.ISO);
                Assert.NotNull(country);
                Assert.Equal(newCountry.Name, country.Name);
                Assert.Equal(newCountry.Created, country.Created);
                Assert.Equal(newCountry.Updated, country.Updated);
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_PageVisit_Update_NoColumns(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                var newVisit = new PageVisit
                {
                    UserID = 1,
                    Date = DateTime.Today,
                    Visits = 1,
                    FirstVisit = _now,
                    LastVisit = _now,
                };

                dbContext.Upsert(newVisit)
                    .On(pv => new { pv.UserID, pv.Date })
                    .Run();

                var visit = dbContext.PageVisits.Single(pv => pv.UserID == newVisit.UserID && pv.Date == newVisit.Date);
                Assert.NotNull(visit);
                Assert.Equal(newVisit.Visits, visit.Visits);
                Assert.Equal(newVisit.FirstVisit, visit.FirstVisit);
                Assert.Equal(newVisit.LastVisit, visit.LastVisit);
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_PageVisit_Update_SelectedColumns(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                var newVisit = new PageVisit
                {
                    UserID = 1,
                    Date = DateTime.Today,
                    Visits = 1,
                    FirstVisit = _now,
                    LastVisit = _now,
                };

                dbContext.Upsert(newVisit)
                    .On(pv => new { pv.UserID, pv.Date })
                    .UpdateColumns(pv => new PageVisit
                    {
                        Visits = pv.Visits + 1,
                        LastVisit = _now,
                    })
                    .Run();

                var visit = dbContext.PageVisits.Single(pv => pv.UserID == newVisit.UserID && pv.Date == newVisit.Date);
                Assert.NotNull(visit);
                Assert.NotEqual(newVisit.Visits, visit.Visits);
                Assert.Equal(_dbVisit.Visits + 1, visit.Visits);
                Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                Assert.Equal(newVisit.LastVisit, visit.LastVisit);
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_DashedTable(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                dbContext.Upsert(new DashTable
                    {
                        DataSet = "test",
                        Updated = _now,
                    })
                    .On(x => x.DataSet)
                    .Run();

                var entry = dbContext.DashTable.Single(x => x.DataSet == "test");
                Assert.NotNull(entry);
            }
        }

        [Theory]
#if USE_POSTGRESQL
        [InlineData(TestDbContext.DbDriver.Postgres)]
#endif
#if USE_SQLSERVER
        [InlineData(TestDbContext.DbDriver.MSSQL)]
#endif
#if USE_MYSQL
        [InlineData(TestDbContext.DbDriver.MySQL)]
#endif
#if USE_SQLITE
        [InlineData(TestDbContext.DbDriver.Sqlite)]
#endif
        public void Upsert_SchemaTable(TestDbContext.DbDriver driver)
        {
            ResetDb(driver);
            using (var dbContext = new TestDbContext(_dataContexts[driver]))
            {
                dbContext.Upsert(new SchemaTable
                    {
                        Name = 1,
                        Updated = _now,
                    })
                    .On(x => x.Name)
                    .Run();

                var entry = dbContext.SchemaTable.Single(x => x.Name == 1);
                Assert.NotNull(entry);
            }
        }
    }
}
