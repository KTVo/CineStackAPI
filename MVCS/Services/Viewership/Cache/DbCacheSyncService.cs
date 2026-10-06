using CineStackAPI.MVCS.Repositories.Viewership.Cache.Interfaces;

namespace CineStackAPI.MVCS.Services.Viewership.Cache;

public sealed class DbCacheSyncService
{
    /*
    TODOS:
    - SAVE WATCHING TO REDIS, THEN SYNC TO MAIN DATABASE
    */
    private readonly IDbCacheRepository _distributedCacheRepo;
    private readonly ILogger<DbCacheSyncService> _logger;

    public DbCacheSyncService(IDbCacheRepository distributedCacheRepo, ILogger<DbCacheSyncService> logger)
    {
        _distributedCacheRepo = distributedCacheRepo ?? throw new ArgumentNullException(nameof(distributedCacheRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    

}
