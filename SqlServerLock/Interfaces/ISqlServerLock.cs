using Microsoft.EntityFrameworkCore;
using SqlServerLock.Utils;

namespace SqlServerLock.Interfaces;

/// <summary>
/// Interface for Sql Sever Lock.
/// </summary>
public interface ISqlServerLock
{
    /// <summary>
    /// Release a previously set lock.
    /// </summary>
    /// <param name="lockName">The name of the lock. Must be excactly the same as when set</param>
    /// <param name="lockOwner">The owner of the lock. Must be excactly the same as when set</param>
    /// <param name="dbContext">The database context for database to use for locks</param>
    /// <returns></returns>
    Result ReleaseLock(string lockName, SqlServerLockOwner lockOwner, DbContext dbContext);

    /// <summary>
    /// Sets a lock.
    /// </summary>
    /// <param name="lockName">The name of the lock</param>
    /// <param name="lockMode">The level of the lock</param>
    /// <param name="lockOwner">The owner of the lock. If you set it to Transaction, then you must have established a transaction</param>
    /// <param name="lockTimeout">Timout in seconds before giving up trying to allocate the lock</param>
    /// <param name="dbContext">The database context for database to use for locks</param>
    /// <returns></returns>
    Result SetLock(string lockName, SqlServerLockMode lockMode, SqlServerLockOwner lockOwner, int lockTimeout, DbContext dbContext);
}
