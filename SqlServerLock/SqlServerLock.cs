using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SqlServerLock.Interfaces;
using SqlServerLock.Utils;
using System.Data;
using System.Data.Common;

namespace SqlServerLock;

public class SqlServerLock(ILogger<SqlServerLock> logger) : ISqlServerLock
{
    const string StoredProcedure_GetAppLock = "sp_getapplock",
        StoredProcedure_ReleaseAppLock = "sp_releaseapplock";

    const string ParamName_Resource = "@Resource",
        ParamName_LockMode = "@LockMode",
        ParamName_LockOwner = "@LockOwner",
        ParamName_LockTimeout = "@LockTimeout",
        ParamName_Result = "@Result";

    readonly SqlServerLockResultLock[] LockOk =
    {
        SqlServerLockResultLock.Granted,
        SqlServerLockResultLock.GrantedAfterWait
    };

    // Results from sp_getapplock
    private enum SqlServerLockResultLock
    {
        Granted = 0,
        GrantedAfterWait = 1,
        RequestTimeout = -1,
        RequestCanceled = -2,
        RequestDeadlock = -3,
        ParmOrCallError = -999
    }

    // Results from sp_relaseapplock
    private enum SqlServerLockResultRelease
    {
        Released = 0,
        ParmOrCallError = -999,
        // Non-standard, converted SQL exception error codes (normally positive,
        // but set negative to comply with other error codes):
        LockNotHeldError = -1223
    }

    public Result SetLock(string lockName, SqlServerLockMode lockMode, SqlServerLockOwner lockOwner, int lockTimeout, DbContext dbContext)
    {
        string methodName = $"{nameof(SetLock)}", paramList = $"('{lockName}', '{lockMode}', '{lockOwner}', '{lockTimeout}', dbContext)";

        try
        {
            var validateParmsResult = ValidateParams(lockName, lockOwner, lockTimeout, dbContext);
            if (validateParmsResult.IsSuccess is false)
            {
                return validateParmsResult;
            }

            var dbConnection = dbContext.Database.GetDbConnection();
            if (dbConnection.State is not ConnectionState.Open)
            {
                dbContext.Database.OpenConnection();
            }
            DbCommand cmd = dbConnection.CreateCommand();

            if (lockOwner is SqlServerLockOwner.Transaction)
            {
                cmd.Transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = StoredProcedure_GetAppLock;
            cmd.CommandTimeout = lockTimeout;  // CommandTimeout is in seconds.
            cmd.Parameters.Add(new SqlParameter(ParamName_Resource, SqlDbType.NVarChar, 255) { Value = lockName });
            cmd.Parameters.Add(new SqlParameter(ParamName_LockMode, SqlDbType.VarChar, 32) { Value = Enum.GetName(lockMode) });
            cmd.Parameters.Add(new SqlParameter(ParamName_LockOwner, SqlDbType.VarChar, 32) { Value = Enum.GetName(lockOwner) });
            cmd.Parameters.Add(new SqlParameter(ParamName_LockTimeout, SqlDbType.Int) { Value = lockTimeout });
            // DbPrincipal is omitted as it will then default to 'public'.
            cmd.Parameters.Add(new SqlParameter(ParamName_Result, SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
            cmd.ExecuteNonQuery();

            var returnCode = cmd.Parameters[ParamName_Result].Value is null ?
                SqlServerLockResultLock.ParmOrCallError : (SqlServerLockResultLock)cmd.Parameters[ParamName_Result].Value!;

            if (!LockOk.Contains(returnCode))
            {
                var res = cmd.Parameters[ParamName_Result].Value is null ? "null" : returnCode.ToString();
                logger.LogError($"Error occurred in '{methodName}{paramList}'. '{StoredProcedure_GetAppLock}' returned '{res}'.");
                return Result.Failure($"Error occurred in '{methodName}'. '{StoredProcedure_GetAppLock}' returned '{res}'.");
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical($"Error occurred in '{methodName}{paramList}'. The error is: '{ex}'.");
            return Result.Failure($"Error occurred in '{methodName}'. The error is: '{ex}'.");
        }
    }


    public Result ReleaseLock(string lockName, SqlServerLockOwner lockOwner, DbContext dbContext)
    {
        string methodName = $"{nameof(ReleaseLock)}", paramList = $"('{lockName}', '{lockOwner}', dbContext)";

        try
        {
            var validateParmsResult = ValidateParams(lockName, lockOwner, 0, dbContext);
            if (validateParmsResult.IsSuccess is false)
            {
                return validateParmsResult;
            }

            var dbConnection = dbContext.Database.GetDbConnection();
            if (dbConnection.State is not ConnectionState.Open)
            {
                logger.LogCritical("The database connection is closed. It must be open in order to release a lock");
                return Result.Failure("The database connection is closed. It must be open in order to release a lock");
            }
            DbCommand cmd = dbConnection.CreateCommand();

            if (lockOwner is SqlServerLockOwner.Transaction)
            {
                cmd.Transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
            }

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = StoredProcedure_ReleaseAppLock;
            cmd.CommandTimeout = 5;
            cmd.Parameters.Add(new SqlParameter(ParamName_Resource, SqlDbType.NVarChar, 255) { Value = lockName });
            cmd.Parameters.Add(new SqlParameter(ParamName_LockOwner, SqlDbType.VarChar, 32) { Value = Enum.GetName(lockOwner) });
            // DbPrincipal is omitted as it will then default to 'public'.
            cmd.Parameters.Add(new SqlParameter(ParamName_Result, SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
            cmd.ExecuteNonQuery();

            var returnCode = cmd.Parameters[ParamName_Result].Value is null ?
                SqlServerLockResultRelease.ParmOrCallError : (SqlServerLockResultRelease)cmd.Parameters[ParamName_Result].Value!;

            if (returnCode is not SqlServerLockResultRelease.Released)
            {
                var res = cmd.Parameters[ParamName_Result].Value is null ? "null" : returnCode.ToString();
                logger.LogError($"Error occurred in '{methodName}{paramList}'. '{StoredProcedure_ReleaseAppLock}' returned '{res}'.");
                return Result.Failure($"Error occurred in '{methodName}'. '{StoredProcedure_ReleaseAppLock}' returned '{res}'.");
            }
            return Result.Success();
        }
        catch (SqlException ex) when (ex.Number == 1223)
        {
            logger.LogCritical($"Error occurred in '{methodName}{paramList}'. '{StoredProcedure_ReleaseAppLock}' gave exception '{SqlServerLockResultRelease.LockNotHeldError}'.");
            return Result.Failure($"Error occurred in '{methodName}'. '{StoredProcedure_ReleaseAppLock}' gave exception '{SqlServerLockResultRelease.LockNotHeldError}'.");
        }
        catch (Exception ex)
        {
            logger.LogCritical($"Error occurred in '{methodName}{paramList}'. The error is: '{ex}'.");
            return Result.Failure($"Error occurred in '{methodName}'. The error is: '{ex}'.");
        }
    }

    /// <summary>
    /// Validates the params and makes sure that relation between params are as required.
    /// </summary>
    /// <param name="lockName">The name of the lock</param>
    /// <param name="lockOwner">The owner of the lock. If you set it to Transaction, then you must have established a transaction</param>
    /// <param name="lockTimeout">Timout in seconds before giving up trying to allocate the lock</param>
    /// <param name="dbContext">The database context for database to use for locks</param>
    /// <returns></returns>
    private Result ValidateParams(string lockName, SqlServerLockOwner lockOwner, int lockTimeout, DbContext dbContext)
    {
        string methodName = $"{nameof(ValidateParams)}", paramList = $"('{lockName}', '{lockOwner}', dbContext)";

        if (string.IsNullOrWhiteSpace(lockName))
        {
            logger.LogCritical($"Error occurred in '{methodName}{paramList}'. The error is: 'lockName is empty or consist of invalid characters'.");
            return Result.Failure($"Error occurred in '{methodName}'. The error is: 'lockName is empty or consist of invalid characters'.");
        }

        if (lockTimeout < 0)
        {
            logger.LogCritical($"Error occurred in '{methodName}{paramList}'. The error is: 'lockTimeout is set to negative value. Must be 0 or positive'.");
            return Result.Failure($"Error occurred in '{methodName}'. The error is: 'lockTimeout is set to negative value. Must be 0 or positive'.");
        }

        if (lockOwner is SqlServerLockOwner.Transaction && dbContext.Database.CurrentTransaction is null)
        {
            logger.LogCritical($"Error occurred in '{methodName}{paramList}'. The error is: 'lockOwner is set to Transaction but no transaction exists'.");
            return Result.Failure($"Error occurred in '{methodName}'. The error is: 'lockOwner is set to Transaction but no transaction exists'.");
        }

        return Result.Success();
    }
}
