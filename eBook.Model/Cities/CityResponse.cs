namespace eBook.Model.Cities;

public class CityResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = null!;
}
