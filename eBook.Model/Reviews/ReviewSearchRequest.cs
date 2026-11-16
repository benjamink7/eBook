using System.ComponentModel.DataAnnotations;
using eBook.Model.Common;

namespace eBook.Model.Reviews;

public class ReviewSearchRequest : PagedRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Knjiga nije ispravna.")]
    public int BookId { get; set; }
}
