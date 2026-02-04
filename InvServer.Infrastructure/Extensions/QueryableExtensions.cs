using InvServer.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InvServer.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResponse<List<T>>> ToPagedResponseAsync<T>(
        this IQueryable<T> query, 
        PagedRequest request)
    {
        var totalRecords = await query.CountAsync();
        var data = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<List<T>>(data, request.PageNumber, request.PageSize, totalRecords);
    }
}
