using System.Collections.Generic;

namespace HealthNote.Application.Emr
{
    public sealed class Page<T>
    {
        public Page(IReadOnlyList<T> items, long totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public IReadOnlyList<T> Items { get; }
        public long TotalCount { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
    }
}
