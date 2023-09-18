using FluentAssertions;

namespace Experimental.Mappings;

public class MappingBenchmarks
{
    private List<Car> _cars;
    private AutoMapper.IMapper _autoMapper;
    private CarMapper _carMapper;
    private Faker<Car> _carFaker;

    [Params(10_000, 100_000, 500_000)]
    public int NumberOfRecords { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var registrationDetailsFaker = new Faker<RegistrationDetails>()
            .RuleFor(x => x.Plate, f => f.Random.Replace("???-####"))
            .RuleFor(x => x.Date, f => f.Date.PastDateOnly(10));

        _carFaker = new Faker<Car>()
            .RuleFor(x => x.Make, f => f.Vehicle.Manufacturer())
            .RuleFor(x => x.Vin, f => f.Vehicle.Vin())
            .RuleFor(x => x.Model, f => f.Vehicle.Model())
            .RuleFor(x => x.Fuel, f => f.Vehicle.Fuel())
            .RuleFor(x => x.Year, f => f.Date.Past(10).Year)
            .RuleFor(x => x.NumberOfSeats, f => f.Random.Int(1, 10))
            .RuleFor(x => x.Color, f => f.Commerce.Color())
            .RuleFor(x => x.Registration, f => registrationDetailsFaker.Generate());
        
        
        
        var config = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Car, CarDto>();
            cfg.CreateMap<RegistrationDetails, RegistrationDetailsDto>();
        });

        _autoMapper = config.CreateMapper();

        _carMapper = new CarMapper();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cars = _carFaker.Generate(NumberOfRecords);
    }

    [Benchmark]
    public void WithAutomapper()
    {
        var dtos = _cars.Select(x => _autoMapper.Map<CarDto>(x)).ToArray();
        dtos.Should().HaveCount(_cars.Count);
    }
    
    [Benchmark]
    public void WithMapperly()
    {
        var dtos = _cars.Select(_carMapper.Map).ToArray();
        dtos.Should().HaveCount(_cars.Count);
    }
}