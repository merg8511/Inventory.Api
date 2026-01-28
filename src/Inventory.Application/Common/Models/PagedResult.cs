namespace Inventory.Application.Common.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Data { get; }
    public PaginationInfo Pagination { get; }
    
    public PagedResult(IReadOnlyList<T> data, int page, int pageSize, int totalCount)
    {
        Data = data;
        Pagination = new PaginationInfo(page, pageSize, totalCount);
    }
}

public record PaginationInfo(int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
