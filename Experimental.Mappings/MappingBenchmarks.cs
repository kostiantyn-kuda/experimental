namespace Experimental.Mappings;

public class MappingBenchmarks
{
    private List<Car> _cars;
    private AutoMapper.IMapper _autoMapper;
    private CarMapper _carMapper;

    [Params(10000, 10000, 100000)]
    public int MaxNumberOfRecords { get; set; }
    
    [IterationSetup]
    public void SetupBenchmark()
    {
        var registrationDetailsFaker = new Faker<RegistrationDetails>()
            .RuleFor(x => x.Plate, f => f.Random.Replace("???-####"))
            .RuleFor(x => x.Date, f => f.Date.PastDateOnly(10));

        var carFaker = new Faker<Car>()
            .RuleFor(x => x.Make, f => f.Vehicle.Manufacturer())
            .RuleFor(x => x.Vin, f => f.Vehicle.Vin())
            .RuleFor(x => x.Model, f => f.Vehicle.Model())
            .RuleFor(x => x.Fuel, f => f.Vehicle.Fuel())
            .RuleFor(x => x.Year, f => f.Date.Past(10).Year)
            .RuleFor(x => x.NumberOfSeats, f => f.Random.Int(1, 10))
            .RuleFor(x => x.Color, f => f.Commerce.Color())
            .RuleFor(x => x.Registration, f => registrationDetailsFaker.Generate());
        
        _cars = carFaker.Generate(MaxNumberOfRecords);
        
        var config = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Car, CarDto>();
            cfg.CreateMap<RegistrationDetails, RegistrationDetailsDto>();
        });

        _autoMapper = config.CreateMapper();

        _carMapper = new CarMapper();
    }

    [Benchmark]
    public void WithAutomapper()
    {
        var dtos = _autoMapper.Map<CarDto[]>(_cars);
    }
    
    [Benchmark]
    public void WithMapperly()
    {
        var dtos = _cars.Select(_carMapper.Map).ToArray();
    }
}