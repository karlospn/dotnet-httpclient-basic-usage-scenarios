# Introduction

This repository contains a series of basic C# scenarios demonstrating how to use the HttpClient functionality correctly and incorrectly.

# **Scenario 1: Create a new HttpClient for every incoming request**

## Source code

- A new ``HttpClient`` is instantiated everytime a new request comes in.
- The ``HttpClient`` is not disposed after being used.

```csharp
[ApiController]
[Route("[controller]")]
public class ScenarioOneController : ControllerBase
{
    
    [HttpGet()]
    public async Task<ActionResult> Get()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"),
            DefaultRequestHeaders = { { "accept", "application/json" } },
            Timeout = TimeSpan.FromSeconds(15)
        };

        var response = await client.GetAsync(
            "posts/1/comments");

        if (response.IsSuccessStatusCode) 
            return Ok(await response.Content.ReadAsStringAsync());

        return StatusCode(500);
    }
}
```

## Pros & cons of this scenario

### Pros
- None

### Cons
- A new ``HttpClient`` is being created everytime a new request comes in, which means that the application has a unnecessary overhead from establishing a new TCP connection every single time.
- If the app is under heavy load this approach can lead to an accumulation of established TCP connections or TCP connections in a ``TIME_WAIT`` state, which can cause a port exhaustion problem.



# **Scenario 2: Create a new HttpClient for every incoming request and dispose of it after use**

- A new ``HttpClient`` is instantiated everytime a new request comes in.
- The ``HttpClient`` is disposed right after being used.

```csharp
[ApiController]
[Route("[controller]")]
public class ScenarioTwoController : ControllerBase
{
    
    [HttpGet()]
    public async Task<ActionResult> Get()
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"),
            DefaultRequestHeaders = { { "accept", "application/json" } },
            Timeout = TimeSpan.FromSeconds(15)
        };

        var response = await client.GetAsync(
            "posts/1/comments");

        if (response.IsSuccessStatusCode)
            return Ok(await response.Content.ReadAsStringAsync());

        return StatusCode(500);
    }
}
```

## Pros & cons of this scenario
### Pros
- In this scenario, it is less likely for the application to suffer from port exhaustion issues. In scenario 1, for each request, the TCP connection would remain in the ``ESTABLISHED`` state for a few minutes until the operating system forced it to close.    
In scenario 2, as we are disposing of the HTTP client right after using it, the connection is closed directly, bypassing the time the connection was hanging around in the ``ESTABLISHED`` state doing nothing.

### Cons
- A new ``HttpClient`` is being created everytime a new request comes in, which means that the application has a unnecessary overhead from establishing a new TCP connection every single time.
- If the rate of requests is high, the app might suffer from port exhaustion issues.



# **Scenario 3: Create a static HttpClient and use it for any incoming requests**

## Source code
- A ``static`` HttpClient instance is created once and utilized for all received requests.

```csharp
[ApiController]
[Route("[controller]")]
public class ScenarioThreeController : ControllerBase
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"),
        DefaultRequestHeaders = { { "accept", "application/json" } },
        Timeout = TimeSpan.FromSeconds(15),
    };

    [HttpGet()]
    public async Task<ActionResult> Get()
    {

        var response = await Client.GetAsync(
            "posts/1/comments");

        if (response.IsSuccessStatusCode)
            return Ok(await response.Content.ReadAsStringAsync());

        return StatusCode(500);
    }
}
```

## Pros & cons of this scenario
### Pros
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue.    
If the rate of requests is very high, the operating system limit of available ports might still be exhausted, but the best way to minimize this issue is exactly what we're doing in this scenario, reusing ``HttpClient`` instances for as many HTTP requests as possible.

### Cons

> You'll see a lot of guidelines mentioning this DNS resolution issue when talking about ``HttpClient``, the truth is that if your app is calling a service that the DNS doesn't change at all, using this approach is perfectly fine.

- HttpClient only resolves DNS entries when a TCP connection is created. If DNS entries changes after a TCP connection has been established, then the client won't notice those updates.    

# **Scenario 4: Create a static or singleton HttpClient with PooledConnectionLifetime and use it for any incoming requests**

## Source code
- A ``static`` HttpClient instance is created once and utilized for all received requests.
- The HttpClient is created using the ``PooledConnectionLifetime`` attribute. This attribute defines how long connections remain active when pooled. Once this lifetime expires, the connection will no longer be pooled or issued for future requests.  

> In the next code snippet, the ``PooledConnectionLifetime`` is set to 15 seconds, which means that TCP connections will cease to be re-issued and be closed after a maximum of 15 seconds. This is highly inefficient and it is only done for demo purposes.

```csharp
[ApiController]
[Route("[controller]")]
public class ScenarioFourController : ControllerBase
{
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromSeconds(10)
    })
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"),
        DefaultRequestHeaders = { { "accept", "application/json" } },
        Timeout = TimeSpan.FromSeconds(15),
    };

    [HttpGet()]
    public async Task<ActionResult> Get()
    {

        var response = await Client.GetAsync(
            "posts/1/comments");

        if (response.IsSuccessStatusCode)
            return Ok(await response.Content.ReadAsStringAsync());

        return StatusCode(500);
    }
}
```

## Pros & cons of this scenario
### Pros
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue. 
- It solves the DNS change issue mentioned on scenario 3.

### Cons
- There are no disadvantages in this scenario.   
It is true that we have to know more and more details to work correctly with ``HttpClient``, but scenario 5 precisely simplifies this entire process.

# **Scenario 5: Use IHttpClientFactory**

## Source code

- An ``IHttpClientFactory`` Named client is setup in the ``Program.cs`` (this scenario uses an IHttpClientFactory named clients, you could use typed client or basic clients and the result will be exactly the same).
- The ``SetHandlerLifetime`` extension method defines the length of time that a ``HttpMessageHandler`` instance can be reused before being discarded. It works almost identical as the ``PooledConnectionLifetime`` attribute from the previous scenario.
- The ``SetHandlerLifetime`` method is set to 15 seconds, which means that TCP connections will cease to be re-issued and be closed after a maximum of 15 seconds. This is highly inefficient and it is only done for demo purposes.
- We use the ``CreateClient`` method from the ``IHttpClientFactory`` to obtain a ``httpClient`` to call our API.


On ``Program.cs``:
```csharp
builder.Services.AddHttpClient("typicode", c =>
            {
                c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
                c.Timeout = TimeSpan.FromSeconds(15);
                c.DefaultRequestHeaders.Add(
                    "accept", "application/json");
            })
            .SetHandlerLifetime(TimeSpan.FromSeconds(15));
````

On ``ScenarioFiveController.cs``:

```csharp
[ApiController]
[Route("[controller]")]
public class ScenarioFiveController : ControllerBase
{
    private readonly IHttpClientFactory _factory;

    public ScenarioFiveController(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [HttpGet()]
    public async Task<ActionResult> Get()
    {
        var client = _factory.CreateClient("typicode");

        var response = await client.GetAsync(
            "posts/1/comments");

        if (response.IsSuccessStatusCode)
            return Ok(await response.Content.ReadAsStringAsync());

        return StatusCode(500);
    }
}
```

## Pros & cons
### Pros
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue. 
- It solves the DNS change issue mentioned on scenario 3.
- It simplifies the declaration and usage of ``HttpClient`` instances.

### Cons
- The IHttpClientFactory keeps everything nice and simple, but it is harder if you need to tweak some additional parameters.   
The next code snippet is an example of how to set the ``PooledConnectionIdleTimeout`` attribute, as you can see you'll need to use the ``ConfigurePrimaryHttpMessageHandler`` extension method and create a new ``SocketsHttpHandler`` instance, just to set the value of the ``PooledConnectionIdleTimeout`` attribute.

```csharp
builder.Services.AddHttpClient("typicode", c =>
    {
        c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
        c.Timeout = TimeSpan.FromSeconds(15);
        c.DefaultRequestHeaders.Add(
            "accept", "application/json");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
    {
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5)
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(20));
```

# **Scenario 6: Use IHttpClientFactory with .NET Framework (and Autofac)** 

> **This is a .NET Framework only scenario**

## Source code

- This scenarios uses Autofac as IoC container.
- An ``IHttpClientFactory`` named client is setup in the ``AutofacWebapiConfig.cs``.
- A few steps are required to setup ``IHttpClientFactory`` with Autofac:
    - Add required packages:
        - ``Microsoft.Extensions.Http``
    - Configure Autofac:
        - Create a new ``ServiceCollection`` instance.
        - Register the ``HttpClient``.
        - Build the ``ServiceProvider`` and resolve ``IHttpClientFactory``.
    - **The ``IHttpClientFactory`` must be setup as ``SingleInstance`` on Autofac**, or it won't work properly.

On ``Global.asax``:
```csharp
protected void Application_Start()
{
    AreaRegistration.RegisterAllAreas();

    AutofacWebapiConfig.Initialize(GlobalConfiguration.Configuration);

    GlobalConfiguration.Configure(WebApiConfig.Register);
    FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
    RouteConfig.RegisterRoutes(RouteTable.Routes);
    BundleConfig.RegisterBundles(BundleTable.Bundles);
}
```

``AutofacWebApiConfig`` class implementation, looks like this:
```csharp
public class AutofacWebapiConfig
{
    public static IContainer Container;

    public static void Initialize(HttpConfiguration config)
    {
        Initialize(config, RegisterServices(new ContainerBuilder()));
    }

    public static void Initialize(HttpConfiguration config, IContainer container)
    {
        config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
    }

    private static IContainer RegisterServices(ContainerBuilder builder)
    {
        builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
        
        builder.Register(ctx =>
        {
            var services = new ServiceCollection();
            services.AddHttpClient("typicode", c =>
            {
                c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
                c.Timeout = TimeSpan.FromSeconds(15);
                c.DefaultRequestHeaders.Add(
                    "accept", "application/json");
            })
            .SetHandlerLifetime(TimeSpan.FromSeconds(15));

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IHttpClientFactory>();

        }).SingleInstance();

        Container = builder.Build();

        return Container;
    }

}
```

``ScenarioSixController.cs`` looks like this:

```csharp
 public class ScenarioSixController : ApiController
{
    private readonly IHttpClientFactory _factory;

    public ScenarioSixController(IHttpClientFactory factory)
    {
        _factory = factory;
    }
    
    public async Task<IHttpActionResult> Get()
    {
        var client = _factory.CreateClient("typicode");

        var response = await client.GetAsync(
            "posts/1/comments");

        if (response.IsSuccessStatusCode)
            return Ok(await response.Content.ReadAsStringAsync());

        return InternalServerError();
    }

}
```

## Pros & cons
### Pros 
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue. 
- It solves the DNS change issues mentioned on scenario 3.

### Cons
- To avoid creating a new TCP connection every time a new request comes in, it is crucial to register the ``IHttpClientFactory`` as a Singleton in Autofac.
