namespace Expiremental.Mappings.Dtos;

public class CarDto
{
    public string Make { get; set; }
    public string Vin { get; set; }
    public string Model { get; set; }
    public string Fuel { get; set; }
    public string Color { get; set; }
    public int Year { get; set; }
    public int NumberOfSeats { get; set; }
    public RegistrationDetailsDto Registration { get; set; }
}