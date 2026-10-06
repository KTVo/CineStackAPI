using CineStackAPI.Helpers.Validation;
using CineStackAPI.MVCS.Models.CRUD;
using CineStackAPI.MVCS.Models.Validation;
using CineStackAPI.MVCS.Repositories.Viewership.Cache.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace CineStackAPI.MVCS.Repositories.Viewership.Cache;

public class DbCacheRepository : IDbCacheRepository
{
     private readonly IDistributedCache _cache;
    private readonly ILogger<DbCacheRepository> _logger;

    public DbCacheRepository(IDistributedCache distributedCache, ILogger<DbCacheRepository> logger)
    {
        // ASSIGNMENTS
        _cache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // NULL CHECKS
        ModelValidationResponse validationResponse = ModelValidationHelpers.Validate<List<object>>(new List<object> { _cache, _logger });

        if (validationResponse.IsSuccess == false)
        {
            throw new ArgumentNullException(validationResponse.Message);
        }
    }
    
    /// <summary>
    /// CREATES OR UPDATES WATCH HISTORY IN THE CACHE
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <param name="expirationInMinutesAbsolute"></param>
    /// <param name="expirationInMinutesSliding"></param>
    /// <returns></returns>
    public async Task<CRUDResponse> CreateOrUpdateWatchHistoryAsync(
        string key,
        string data,
        int expirationInMinutesAbsolute = 30,
        int expirationInMinutesSliding = 30)
    {
        try
        {
            // Cache the data with options.
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationInMinutesAbsolute),
                SlidingExpiration = TimeSpan.FromMinutes(expirationInMinutesSliding)
            };

            await _cache.SetStringAsync(key: key, value: data, options: options);
            return new CRUDResponse { IsSuccess = true, Message = "WATCH HISTORY CACHE CREATED / UPDATED SUCCESSFULLY" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new CRUDResponse { IsSuccess = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// READS WATCH HISTORY FROM CACHE
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<CRUDResponse> ReadWatchHistoryAsync(string key)
    {
        try
        {
            string? data = await _cache.GetStringAsync(key);

            if (data == null)
            {
                return new CRUDResponse { IsSuccess = false, Message = "WATCH HISTORY NOT FOUND" };
            }
            return new CRUDResponse { DataAsString = data, IsSuccess = true, Message = "WATCH HISTORY RETRIEVED SUCCESSFULLY." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new CRUDResponse { IsSuccess = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// DELETES WATCH HISTORY FROM CACHE
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<CRUDResponse> DeleteWatchHistoryAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            return new CRUDResponse { IsSuccess = true, Message = "WATCH HISTORY DELETED SUCCESSFULLY!" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new CRUDResponse { IsSuccess = false, Message = ex.Message };
        }
    }
}
