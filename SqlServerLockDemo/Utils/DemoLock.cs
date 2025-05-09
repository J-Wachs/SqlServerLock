using Microsoft.EntityFrameworkCore;
using SqlServerLock;
using SqlServerLock.Interfaces;
using SqlServerLock.Utils;
using SqlServerLockDemo.Utils.Interfaces;

namespace SqlServerLockDemo.Utils;

/// <summary>
/// Class for the Demo lock.
/// </summary>
/// <param name="sqlServerLock">DI of the Sql Server Lock base class</param>
public class DemoLock(ISqlServerLock sqlServerLock) : IDemoLock
{
    private const string lockName = "DemoLock";
    private const SqlServerLockMode lockMode = SqlServerLockMode.Exclusive;
    //private const SqlServerLockMode lockMode = SqlServerLockMode.Shared;
    private const SqlServerLockOwner lockOwner = SqlServerLockOwner.Transaction;
    private const int lockTimeout = 2; // Seconds

    public Result Set(DbContext dbContext)
    {
        return sqlServerLock.SetLock(lockName, lockMode, lockOwner, lockTimeout, dbContext);
    }

    public Result Release(DbContext dbContext)
    {
        return sqlServerLock.ReleaseLock(lockName, lockOwner, dbContext);
    }
}
