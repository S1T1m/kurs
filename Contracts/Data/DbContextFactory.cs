using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Contracts.Data;

public sealed class DbContextFactory
{
    private readonly string _cs;

    public DbContextFactory(string dbPath)
    {
        
        var b = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            ForeignKeys = true
        };
        _cs = b.ToString();
    }

    public ContractsDbContext Create()
        => new(new DbContextOptionsBuilder<ContractsDbContext>()
            .UseSqlite(_cs)
            .Options);
}