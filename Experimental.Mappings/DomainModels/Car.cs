namespace Expiremental.Mappings.DomainModels;

public class Car
{
    public string Make { get; set; }
    public string Vin { get; set; }
    public string Model { get; set; }
    public string Fuel { get; set; }
    public string Color { get; set; }
    public int Year { get; set; }
    public int NumberOfSeats { get; set; }
    public RegistrationDetails Registration { get; set; }
}