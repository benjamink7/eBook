namespace eBook.Model.Countries;

public class CountryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int CityCount { get; set; }
}
