# Sql Server Lock service

Where the C# statement 'lock' works within an application, the Sql Server Lock service (distributed lock)
allows you to use locks across processes (applications and servers). The locks are managed by a Microsoft
Sql Server.

The service has been developed in .NET 8.

## Fully functional example project

This repo contains a project that is the actual service, support classes and a demo project,
where you can see an example of how the Sql Server Lock works.

## How does it work?

After having setup the connection string to the Microsoft Sql Server to use, your application calls methods in this service
to set and release locks.

This service wraps the two stored procedures 'sp_getapplock' and 'sp_releaseapplock' in the Sql Server. If it is not possible
to set or release the lock, an error is returned to your application.

Setting a lock require a number of parameters to be set:

* Name of lock: This is a name you give the lock.
* Lock mode: The mode of the lock: 1) Shared, 2) Update, 3) IntentShared, 4) IntentExclusive, 5) Exclusive.
* Lock owner: The lock owner can be one of two types: 1) Session, 2) Transaction. What to use, depend on your secnario.
* Timeout value: Number of seconds to wait for trying to get a lock. If it is not possible to obtain the lock within this time frame, an
* error is returned.

For more details about these parameters, please see the Microsoft documentation:
sp_getapplock: https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver16

sp_releaselock: https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-releaseapplock-transact-sql?view=sql-server-ver16

## Installation instructions

### Getting and trying out the Sql Server Lock service
Download the repo and open the demo project in Visual Studio. Then change the connection string in appsetting.json to point to your Sql server.

Then run the demo project, and click on the buttons. Messages are displayed as the demo progress.

Note in the demo project, that one of the demos uses a Sql Server Lock that wraps the actual Sql Sever lock methods.

### Setting up your own project to use the Sql Server Lock service
To use the Sql Server Lock service in your own projects, you must add the service project to your solution, or copy over the files needed.
Then you to add the service to the Program.cs in
your main project. Example from demo project:

```
// Added for SqlServerLock:
builder.Services.AddScoped<ISqlServerLock, SqlServerLock.SqlServerLock>();
builder.Services.AddScoped<IDemoLock, DemoLock>();

builder.Services.AddDbContext<DbContext>(options =>
{
   options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
// End Added for SqlServerLock.
```

## Found a bug?

Please create an issue in the repo.

## Known issues (Work in progress)

None at this time.

## FAQ

### I want to use the service on another brand of Sql Server, what do I do?
The two stored procedures that are wrapped, are specific to Microsoft Sql Server. If you want a similar functionality from other brands
of Sql Server, then you need to look into if and how this is done on that brand of Sql Server.
 