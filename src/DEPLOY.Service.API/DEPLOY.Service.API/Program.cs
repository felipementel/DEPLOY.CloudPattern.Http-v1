using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Registry;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Latency;
using Polly.Telemetry;
using Scalar.AspNetCore;
using System.Diagnostics;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

//var predicateBuilder = new PredicateBuilder<HttpResponseMessage>()
//         .Handle<HttpRequestException>()
//         .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError);

var telemetryOptions = new TelemetryOptions
{
    // Configure logging
    LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole())
};

// Configure enrichers
telemetryOptions.MeteringEnrichers.Add(new MyMeteringEnricher());

// Configure telemetry listeners
telemetryOptions.TelemetryListeners.Add(new MyTelemetryListener());


var optionsOnFallback = new FallbackStrategyOptions<News>
{
    ShouldHandle = new PredicateBuilder<News>()
        //.Handle<SomeExceptionType>()
        .HandleResult(r => r is null),
    FallbackAction = static args =>
    {
        //var avatar = News..GetRandomAvatar();
        return Outcome.FromResultAsValueTask(News.Blank);
    },
    OnFallback = static args =>
    {
        // Add extra logic to be executed when the fallback is triggered, such as logging.
        return default; // Returns an empty ValueTask
    }
};



builder.Services.AddHttpClient("canalDEPLOY-Service", client =>
{
    client.BaseAddress = new Uri("http://localhost:7119");
});

builder.Services.AddResiliencePipeline<string, News>("canalDEPLOY-Fallback", builder =>
{
    builder.AddFallback(new FallbackStrategyOptions<News>
    {
        Name = "CanalDEPLOY-Fallback",
        OnFallback = args =>
        {
            Debug.WriteLine("Fallback! Outcome: {0}.", args.Outcome);
            return default;
        },
        FallbackAction = args => Outcome.FromResultAsValueTask<News>(new News("não verificado"))
    });
});

builder.Services.AddResiliencePipeline<HttpResponseMessage>("canalDEPLOY-Pipeline-2", (configure, context) =>
{
    configure.Name = "canalDEPLOY-pipeline";
    configure.InstanceName = "canalDEPLOY-instance";
    configure
    // Adicione o Fallback como primeira estratégia no pipeline
    .AddFallback<string>(new FallbackStrategyOptions<string>
    {
        Name = "CanalDEPLOY-FallbackStrategy",
        //ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        FallbackAction = static args =>
        {
            var avatar = Random.Shared.Next();
            return Outcome.FromResultAsValueTask(avatar.ToString());
        },
        OnFallback = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Fallback acionado devido a: {args.Outcome.Exception?.Message}");
            Console.WriteLine();
            return default;
        }
    })
    .AddRetry(new RetryStrategyOptions
    {
        Name = "CanalDEPLOY-RetryStrategy",
        //ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        Delay = TimeSpan.FromSeconds(1),
        MaxRetryAttempts = 5,
        BackoffType = DelayBackoffType.Constant,
        OnRetry = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Retry attempt {args.AttemptNumber} | {args.Context.OperationKey}");
            Console.WriteLine();
            return default;
        }
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        Name = "CanalDEPLOY-CircuitBreakerStrategy",
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        //BreakDurationGenerator = static args => new ValueTask<TimeSpan>(TimeSpan.FromSeconds(args.FailureCount)),
        SamplingDuration = TimeSpan.FromSeconds(60),
        FailureRatio = 0.7,
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(60),
        OnOpened = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Circuit breaker is open. 1 {args.Context.OperationKey} 2 {args.Context.Properties} 3 {args.Outcome.Result}");
            Console.WriteLine();
            return default;
        },
        OnHalfOpened = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Circuit breaker is half open.");
            Console.WriteLine();
            return default;
        },
        OnClosed = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Circuit breaker is closed.");
            Console.WriteLine();
            return default;
        }
    })
    .AddTimeout(TimeSpan.FromSeconds(300));
});











builder.Services.AddResiliencePipeline("canalDEPLOY-Pipeline", (configure, context) =>
{
    configure.Name = "canalDEPLOY-pipeline";
    configure.InstanceName = "canalDEPLOY-instance";
    configure
    .AddRetry(new RetryStrategyOptions
    {
        Name = "CanalDEPLOY-RetryStrategy",
        //ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        Delay = TimeSpan.FromSeconds(1),
        MaxRetryAttempts = 5,
        BackoffType = DelayBackoffType.Constant,
        OnRetry = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Retry attempt {args.AttemptNumber} | {args.Context.OperationKey}");
            Console.WriteLine();
            return default;
        }
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        Name = "CanalDEPLOY-CircuitBreakerStrategy",
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        //BreakDurationGenerator = static args => new ValueTask<TimeSpan>(TimeSpan.FromSeconds(args.FailureCount)),
        SamplingDuration = TimeSpan.FromSeconds(60), //O período de tempo durante a taxa de falha-sucesso é calculada.
        FailureRatio = 0.7,
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(60),
        OnOpened = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Circuit breaker is open. 1 {args.Context.OperationKey} 2 {args.Context.Properties} 3 {args.Outcome.Result}");
            Console.WriteLine();
            return default;
        },
        OnHalfOpened = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Circuit breaker is half open.");
            Console.WriteLine();
            return default;
        },
        OnClosed = static args =>
        {
            Console.WriteLine();
            Console.WriteLine($"Circuit breaker is closed.");
            Console.WriteLine();
            return default;
        }
    })
    .AddTimeout(TimeSpan.FromSeconds(300)); // 5 min
    //.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
    // {
    //     Name = "CanalDEPLOY-FallbackStrategy",
    //     //ShouldHandle = new PredicateBuilder().Handle<Exception>(),
    //     FallbackAction = static args =>
    //     {
    //         Console.WriteLine("Fallback action executed.");
    //         var fallbackResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
    //         {
    //             Content = new StringContent("Fallback response")
    //         };
    //         //return new ValueTask<HttpResponseMessage>(fallbackResponse);
    //         return new ValueTask<Polly.Outcome<HttpResponseMessage>>(Outcome.FromResult(fallbackResponse));
    //     }
    // });

    if (builder.Environment.IsDevelopment())
    {
        //configure
        //.AddChaosLatency(new ChaosLatencyStrategyOptions
        //{
        //    Name = "CanalDEPLOY-ChaosLatencyStrategy",
        //    LatencyGenerator = static args =>
        //    {
        //        TimeSpan latency = args.Context.OperationKey switch
        //        {
        //            "DataLayer" => TimeSpan.FromMilliseconds(500),
        //            "ApplicationLayer" => TimeSpan.FromSeconds(2),
        //            _ => TimeSpan.FromSeconds(7)//TimeSpan.Zero
        //        };

        //        Console.WriteLine($"Generated latency: {latency} for OperationKey: {args.Context.OperationKey}");
        //        return new ValueTask<TimeSpan>(latency);
        //    },
        //    InjectionRate = 0.7,
        //    OnLatencyInjected = static args =>
        //    {
        //        Console.WriteLine($"OnLatencyInjected, Latency: {args.Latency}, Operation: {args.Context.OperationKey}.");
        //        return default;
        //    }
        //})
        //.AddChaosFault(0.4, () => new InvalidOperationException("Injected by chaos strategy!")) // Inject a chaos fault to executions
        //.AddChaosBehavior(0.001, cancellationToken => RestartRedisAsync(cancellationToken)) // Inject a chaos behavior to executions
        //.ConfigureTelemetry(telemetryOptions); // This method enables telemetry in the builder
    }

});
//    .Configure<TelemetryOptions>(options =>
//{
//    // Configure enrichers
//    options.MeteringEnrichers.Add(new MyMeteringEnricher());

//    // Configure telemetry listeners
//    options.TelemetryListeners.Add(new MyTelemetryListener());
//});



async ValueTask RestartRedisAsync(CancellationToken cancellationToken)
{
    throw new NotImplementedException();
}

static async Task<Outcome<News>> CallSecondary(CancellationToken cancellationToken)
{
    await Task.Delay(1000, cancellationToken);
    return Outcome.FromResult(new News { Title = "teste" });
}

var app = builder.Build();

//{
//    // Configure enrichers
//    options.MeteringEnrichers.Add(new MyMeteringEnricher());

//    // Configure telemetry listeners
//    options.TelemetryListeners.Add(new MyTelemetryListener());
//});

//public async ValueTask RestartRedisAsync(CancellationToken cancellationToken)
//{
//    return await Task.CompletedTask;
//}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}


app.MapGet("/get", async (
    IHttpClientFactory clientFactory,
    ResiliencePipelineProvider<string> pipelineProvider,
    CancellationToken cancellationToken) =>
{
    using var client = clientFactory.CreateClient("canalDEPLOY-Service");

    var pipeline = pipelineProvider.GetPipeline("canalDEPLOY-Pipeline");

    var fallback = pipelineProvider.GetPipeline<News>("canalDEPLOY-Fallback");

    //how to build pipeline and fallback in the same time

    ResilienceContext resilienceContext = ResilienceContextPool.Shared.Get("CanalDEPLOY-OperationKey");





    var response = await pipeline.ExecuteAsync(async (context, cancelationToken) =>
    {
        var response = await client.GetAsync("canal-deploy/api/ListJobs", cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }, resilienceContext);

    return response.Content.ReadFromJsonAsAsyncEnumerable<Jobs>(cancellationToken: cancellationToken);
});


await app.RunAsync();

internal sealed class MyTelemetryListener : TelemetryListener
{
    public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
    {
        Console.WriteLine($"Telemetry event occurred: {args.Event.EventName}");
    }
}

internal sealed class MyMeteringEnricher : MeteringEnricher
{
    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        context.Tags.Add(new("my-custom-tag", "custom-value"));
    }
}


public class Jobs
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}



public class News
{
    public News()
    {

    }

    public News(string title)
    {
        Title = title;
    }

    public static readonly News Blank = new();

    public string Title { get; set; }

    public async Task<string> GetNews()
    {
        return await Task.FromResult("public news");
    }
}