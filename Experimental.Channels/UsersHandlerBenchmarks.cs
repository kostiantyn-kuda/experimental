namespace Experimental.Channels;

//[Orderer(SummaryOrderPolicy.Method)]
public class UsersHandlerBenchmarks
{
    private readonly Faker<User> _userFaker;
    private User[] _users;
    private string _outputDirectory;

    [Params(10, 100, 1000)]
    public int MaxNumberOfUsers { get; set; }
    
    [Params(5, 10, 100)]
    public int PageSize { get; set; }
    
    public IEnumerable<object[]> ChannelsSource() => new List<object[]>
    {
        new object[] { 1, 1 },
        new object[] { 5, 5 },
        new object[] { 10, 5 },
        new object[] { 20, 5 }
    };
    
    public UsersHandlerBenchmarks()
    {
        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.FirstName, f => f.Person.FirstName)
            .RuleFor(u => u.LastName, f => f.Person.LastName)
            .RuleFor(u => u.Age, f => f.Random.Int(18, 80))
            .RuleFor(u => u.AvatarUrl, f => f.Person.Avatar);
    }

    [IterationSetup]
    public void SetupBenchmark()
    {
        _outputDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "output"
        );
        
        _users = _userFaker
            .Generate(MaxNumberOfUsers)
            .ToArray();
        
        //ReCreateDirectory(_outputDirectory);
    }

    [Benchmark]
    //[Description("Async Handling In One Thread")]
    public async Task AsyncHandlingInOneThread()
    {
        var page = 0;
        User[] users;
        
        using var httpClient = new HttpClient();
        do
        {
            users = await GetUsers(page * PageSize, PageSize);
            foreach (var user in users)
            {
                Console.WriteLine($"Downloading image for user {user.Id} {user.AvatarUrl}");
                var bytes = await DownloadImage(user.AvatarUrl, httpClient);
                
                Console.WriteLine($"Persisting user {user.Id}");
                await SaveUser(_outputDirectory, user, bytes);
            }
        
            page++;
        } 
        while (users.Any());
    }
    
    [Benchmark]
    //[Description("Async Handling In Parallel")]
    public async Task AsyncHandlingWithParallel()
    {
        var page = 0;
        User[] users;
        
        using var httpClient = new HttpClient();
        do
        {
            users = await GetUsers(page * PageSize, PageSize);

            await Parallel.ForEachAsync(users, async (user, token) =>
            {
                Console.WriteLine($"Downloading image for user {user.Id} {user.AvatarUrl}");
                var bytes = await DownloadImage(user.AvatarUrl, httpClient);
                
                Console.WriteLine($"Persisting user {user.Id}");
                await SaveUser(_outputDirectory, user, bytes);
            });
        
            page++;
        } 
        while (users.Any());
    }

    [Benchmark]
    [ArgumentsSource(nameof(ChannelsSource))]
    //[Description("Async Handling With Channels")]
    public async Task AsyncHandlingWithChannels(int numberOfDownloaders, int numberOfPersisters)
    {
        var userChannel = Channel.CreateUnbounded<User>();
        var userWithImageChannel = Channel.CreateUnbounded<(User user, byte[] imageBytes)>();

        var tasks = new List<Task>();

        // providing data
        tasks.Add(
            Task.Run(async () =>
            {
                var page = 0;
                User[] users;
                do
                {
                    users = await GetUsers(page * PageSize, PageSize);
                    foreach (var user in users)
                    {
                        await userChannel.Writer.WriteAsync(user);
                    }
                    page++;
                } 
                while (users.Any());

                userChannel.Writer.Complete();
            })
        );
        
        // downloading
        tasks.Add(
            Task.Run(async () =>
            {
                var internalTasks = new List<Task>(
                    Enumerable.Range(1, numberOfDownloaders).Select(x =>
                        // image downloader(s)
                        Task.Run(async () =>
                        {
                            using var httpClient = new HttpClient();

                            var channelReader = userChannel.Reader;

                            while (await channelReader.WaitToReadAsync())
                            {
                                while (channelReader.TryRead(out User item))
                                {
                                    Console.WriteLine($"{x} Downloading image for user {item.Id} {item.AvatarUrl}");
                                    var bytes = await DownloadImage(item.AvatarUrl, httpClient);

                                    await userWithImageChannel.Writer.WriteAsync(
                                        (item, bytes)
                                    );
                                }
                            }
                        })
                    )    
                );

                await Task.WhenAll(internalTasks);
                    
                userWithImageChannel.Writer.Complete();
            })    
        );

        // data persistence
        tasks.AddRange(
            Enumerable.Range(1, numberOfPersisters).Select(x => 
                Task.Run(async () =>
                {
                    var channelReader = userWithImageChannel.Reader;

                    while (await channelReader.WaitToReadAsync())
                    {
                        while (channelReader.TryRead(out (User user, byte[] imageBytes) data))
                        {
                            Console.WriteLine($"Persisting user {data.user.Id}");
                            await SaveUser(_outputDirectory, data.user, data.imageBytes);
                        }
                    }
                })                
            )
        );

        await Task.WhenAll(tasks);
    }

    private Task<User[]> GetUsers(int skip, int take)
    {
        return Task.FromResult(
            _users.Skip(skip).Take(take).ToArray()
        );
    }
    
    async Task<byte[]> DownloadImage(string imageUrl, HttpClient httpClient)
    {
        // var response = await httpClient.GetAsync(imageUrl);
        // var imageBytes = await response.Content.ReadAsByteArrayAsync();
        // return imageBytes;
        
        await Task.Delay(
            TimeSpan.FromMilliseconds(30)
        );
        return Array.Empty<byte>();
    }

    Task SaveUser(string directory, User user, byte[] imageBytes)
    {
        // var userOutputDirectory = Path.Combine(directory, user.Id.ToString());
        //
        // ReCreateDirectory(userOutputDirectory);
        //
        // var fileJsonPath = Path.Combine(userOutputDirectory, "user.json");
        // await File.WriteAllTextAsync(fileJsonPath, JsonConvert.SerializeObject(user));
        //
        // var fileImagePath = Path.Combine(userOutputDirectory, "avatar.jpg");
        // await File.WriteAllBytesAsync(fileImagePath, imageBytes);

        return Task.Delay(
            TimeSpan.FromMilliseconds(5)
        );
    }
    
    // void ReCreateDirectory(string directoryPath)
    // {
    //     if (Directory.Exists(directoryPath))
    //     {
    //         Directory.Delete(directoryPath, true);
    //     }
    //     Directory.CreateDirectory(directoryPath);
    // }
}

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } 
    public string LastName { get; set; }
    public int Age { get; set; }
    public string AvatarUrl { get; set; }
}