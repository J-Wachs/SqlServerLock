using Microsoft.EntityFrameworkCore;
using SqlServerLock.Utils;

namespace SqlServerLockDemo.Utils.Interfaces;

public interface IDemoLock
{
    /// <summary>
    /// Release the Demo lock.
    /// </summary>
    /// <param name="dbContext">DbContext to use</param>
    /// <returns></returns>
    Result Release(DbContext dbContext);

    /// <summary>
    /// Set the Demo lock.
    /// </summary>
    /// <param name="dbContext">DbContext to use</param>
    /// <returns></returns>
    Result Set(DbContext dbContext);
}
