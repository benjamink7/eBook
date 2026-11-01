using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Common;

public class PagedRequest
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Broj stranice mora biti najmanje 1.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Veličina stranice mora biti između 1 i 100.")]
    public int PageSize { get; set; } = 20;
}
