namespace SqlServerLock;

public enum SqlServerLockMode
{
    Shared,
    Update,
    IntentShared,
    IntentExclusive,
    Exclusive
}
