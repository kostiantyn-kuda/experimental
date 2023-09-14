using Experimental.Channels.Models;

var builder = CoconaApp.CreateBuilder();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddHttpClient();

var app = builder.Build();

var outputDirectory = Path.Combine(
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
    "output"
);

var numberOfUsersPerPage = 10;
    
ReCreateDirectory(outputDirectory);

// app.AddCommand(async (IUserService userService, IHttpClientFactory httpClientFactory, CoconaAppContext ctx) =>
// {
//     var page = 0;
//     IEnumerable<User> users;
//     do
//     {
//         ctx.CancellationToken.ThrowIfCancellationRequested();
//         
//         var usersEnumeration = await userService.GetUsers(page * numberOfUsersPerPage, numberOfUsersPerPage);
//         users = usersEnumeration.ToArray();
//
//         foreach (var user in users)
//         {
//             var bytes = await DownloadImage(user.AvatarUrl, httpClientFactory.CreateClient(), ctx.CancellationToken);
//             await SaveUser(outputDirectory, user, bytes, ctx.CancellationToken);
//         }
//         
//         page++;
//     } 
//     while (users.Any());
// });

app.AddCommand(async (IUserService userService, IHttpClientFactory httpClientFactory, CoconaAppContext ctx) =>
{
    var userChannel = Channel.CreateUnbounded<User>();
    var userWithImageChannel = Channel.CreateUnbounded<(User user, byte[] imageBytes)>();

    var tasks = new List<Task>();
    
    tasks.Add(
        // data provider
        Task.Run(async () =>
        {
            var page = 0;
            IEnumerable<User> users;
            do
            {
                var usersEnumeration = await userService.GetUsers(page * numberOfUsersPerPage, numberOfUsersPerPage);
                users = usersEnumeration.ToArray();
                foreach (var user in users)
                {
                    await userChannel.Writer.WriteAsync(user, ctx.CancellationToken);
                }
                page++;
            } 
            while (users.Any());

            userChannel.Writer.Complete();
        })    
    );
    
    var numberOfDownloaders = 20;
    tasks.AddRange(
        Enumerable.Range(1, numberOfDownloaders).Select(x =>
            // image downloader(s)
            Task.Run(async () =>
            {
                await foreach (var user in userChannel.Reader.ReadAllAsync(ctx.CancellationToken))
                {
                    Console.WriteLine($"{x} Downloading image for user {user.Id} {user.AvatarUrl}");
                    var bytes = await DownloadImage(user.AvatarUrl, httpClientFactory.CreateClient(), ctx.CancellationToken);

                    await userWithImageChannel.Writer.WriteAsync(
                        (user, bytes), ctx.CancellationToken
                    );
                }
                
                userWithImageChannel.Writer.Complete();
            })
        )
    );
    
    tasks.Add(
        // data persistence
        Task.Run(async () =>
        {
            await foreach (var data in userWithImageChannel.Reader.ReadAllAsync(ctx.CancellationToken))
            {
                await SaveUser(outputDirectory, data.user, data.imageBytes, ctx.CancellationToken);                
            }
        })   
    );

    await Task.WhenAll(tasks);
});

var stopWatch = Stopwatch.StartNew();

await app.RunAsync();

stopWatch.Stop();
Console.WriteLine("Elapsed time: {0}", stopWatch.Elapsed);
Console.ReadLine();

async Task<byte[]> DownloadImage(string imageUrl, HttpClient httpClient, CancellationToken cancellationToken)
{
    var response = await httpClient.GetAsync(imageUrl, cancellationToken);
    var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    return imageBytes;
}

async Task SaveUser(string directory, User user, byte[] imageBytes, CancellationToken cancellationToken)
{
    var userOutputDirectory = Path.Combine(directory, user.Id.ToString());
    
    ReCreateDirectory(userOutputDirectory);

    var fileJsonPath = Path.Combine(userOutputDirectory, "user.json");
    await File.WriteAllTextAsync(fileJsonPath, JsonConvert.SerializeObject(user), cancellationToken);

    var fileImagePath = Path.Combine(userOutputDirectory, "avatar.jpg");
    await File.WriteAllBytesAsync(fileImagePath, imageBytes, cancellationToken);
}

void ReCreateDirectory(string directoryPath)
{
    if (Directory.Exists(directoryPath))
    {
        Directory.Delete(directoryPath, true);
    }
    Directory.CreateDirectory(directoryPath);
}