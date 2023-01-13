using System.Net;
using System.Runtime.CompilerServices;
using Alba;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        DiffEngine.DiffRunner.Disabled = true;
    }
}

[UsesVerify]
public class UnitTest1
{
    [Fact]
    public async Task Schema()
    {
        await using var app = await AlbaHost.For<Program>(new LocalExtension());

        var executor = await app.Services.GetRequestExecutorAsync();
        await Verify(executor.Schema.Print(), "graphql");
    }

    [Fact]
    public async Task GetAuthor_Good()
    {
        await using var app = await AlbaHost.For<Program>(new LocalExtension());

        var client = app.Services.GetRequiredService<IMyClient>();
        var authorResponse = await client.Author.ExecuteAsync();
        await Verify(authorResponse);
    }

    [Fact]
    public async Task GetBook_Good()
    {
        await using var app = await AlbaHost.For<Program>(new LocalExtension(), new FixHotChocolateExtension());

        var client = app.Services.GetRequiredService<IMyClient>();
        var authorResponse = await client.Book.ExecuteAsync();
        await Verify(authorResponse);
    }

    [Fact]
    public async Task GetBoth_Good()
    {
        await using var app = await AlbaHost.For<Program>(new LocalExtension(), new FixHotChocolateExtension());

        var client = app.Services.GetRequiredService<IMyClient>();
        var authorResponse = await client.Both.ExecuteAsync();
        await Verify(authorResponse);
    }

    [Fact]
    public async Task GetBook_Bad()
    {
        await using var app = await AlbaHost.For<Program>(new LocalExtension());

        var client = app.Services.GetRequiredService<IMyClient>();
        var authorResponse = await client.Book.ExecuteAsync();
        await Verify(authorResponse);
    }

    [Fact]
    public async Task GetBoth_Bad()
    {
        await using var app = await AlbaHost.For<Program>(new LocalExtension());

        var client = app.Services.GetRequiredService<IMyClient>();
        var authorResponse = await client.Both.ExecuteAsync();
        await Verify(authorResponse);
    }
}

class FixHotChocolateExtension : IAlbaExtension
{
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task Start(IAlbaHost host)
    {
        return Task.CompletedTask;
    }

    public IHostBuilder Configure(IHostBuilder builder)
    {
        builder.ConfigureServices(z => z.AddHttpResponseFormatter<MyHttpResponseFormatter>());

        return builder;
    }
}

class MyHttpResponseFormatter : DefaultHttpResponseFormatter
{
    public MyHttpResponseFormatter() : base(true)
    {
    }

    protected override HttpStatusCode GetStatusCode(IResponseStream responseStream, FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        var code = base.GetStatusCode(responseStream, format, proposedStatusCode);
        return code == HttpStatusCode.InternalServerError ? HttpStatusCode.OK : code;
    }

    protected override HttpStatusCode GetStatusCode(IQueryResult result, FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        var code = base.GetStatusCode(result, format, proposedStatusCode);
        return code == HttpStatusCode.InternalServerError ? HttpStatusCode.OK : code;
    }
}

class LocalExtension : IAlbaExtension
{
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task Start(IAlbaHost host)
    {
        return Task.CompletedTask;
    }

    public class CO : PostConfigureOptions<HttpClientFactoryOptions>
    {
        private readonly TestServer _testServer;

        public CO(TestServer testServer) : base(Options.DefaultName, null)
        {
            _testServer = testServer;
        }

        public override void PostConfigure(string name, HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(
                builder => builder.PrimaryHandler = _testServer.CreateHandler()
            );

            options.HttpClientActions.Add(
                client => client.BaseAddress = new Uri(_testServer.BaseAddress + "graphql/")
            );
        }
    }

    public IHostBuilder Configure(IHostBuilder builder)
    {
        builder.ConfigureServices(s => s.AddSingleton<TestServer>(z => (TestServer)z.GetRequiredService<IServer>()));
        builder.ConfigureServices(
            s =>
            {
                s.AddHttpClient();
                s.AddMyClient();
                // s.AddTestsClient();
                // s.AddRocketClient();
                s.ConfigureOptions<CO>();
            }
        );

        return builder;
    }
}
