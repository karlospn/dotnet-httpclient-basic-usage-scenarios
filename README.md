# Introduction

This repository contains a series of basic C# scenarios demonstrating how to use the HttpClient functionality correctly and incorrectly.

# **Scenario 1: Create a new HttpClient for every incoming request**

## Source code

- A new ``HttpClient`` is instantiated every time a new request comes in.
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
## Pros & cons from this scenario

### Pros
- None

### Cons
- A new ``HttpClient`` is being created every time a new request comes in, which means that the application has an unnecessary overhead from establishing a new TCP connection for every single request.
- If the app is under heavy load this approach can lead to an accumulation of TCP connections on a ``ESTABLISHED`` state or in a ``TIME_WAIT`` state, which can cause a port exhaustion problem.

# **Scenario 2: Create a new HttpClient for every incoming request and dispose of it after use**

- A new ``HttpClient`` is instantiated every time a new request comes in.
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

## Pros & cons from this scenario
### Pros
- In this scenario, it is less likely for the application to experience port exhaustion issues.   
In scenario 1, for each request, the TCP connection would remain in an ``ESTABLISHED`` state for a few minutes until the operating system forced it to close.    
In contrast, in scenario 2, since we are disposing of the HTTP client after its use, the connection is promptly closed, eliminating the period of time during which the connection was lingering in an ``ESTABLISHED`` state.

### Cons
- A new ``HttpClient`` is being created every time a new request comes in, which means that the application has an unnecessary overhead from establishing a new TCP connection every single time.
- In this scenario, although we have managed to eliminate the fact that TCP connections remain in an  ``ESTABLISHED`` state for a couple of minutes, we are still creating a new TCP connection for each incoming request the controller receives. This situation could still potentially result in issues related to port exhaustion, particularly if the application experiences a high volume of traffic.


# **Scenario 3: Create a static HttpClient and use it for any incoming requests**

## Source code
- A ``static`` ``HttpClient`` instance is created once and reused for incoming requests.

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

## Pros & cons from this scenario
### Pros 
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue.    
If the rate of requests is very high, the operating system limit of available ports might still be exhausted, but the best way to minimize this issue is exactly what we're doing in this scenario, reusing ``HttpClient`` instances for as many HTTP requests as possible.

### Cons

> You'll see a lot of guidelines mentioning this DNS resolution issue when talking about ``HttpClient``. The truth is, if your app is making calls to a service where you're aware that the DNS address won't change at all, using this approach is perfectly fine.

- ``HttpClient`` only resolves DNS entries when a TCP connection is created. If DNS entries changes regularly, then the client won't notice those updates.    

# **Scenario 4: Create a static or singleton HttpClient with PooledConnectionLifetime and use it for any incoming requests**

## Source code
- A ``static`` ``HttpClient`` instance is created once and reused for incoming requests.
- The ``HttpClient`` is created using the ``PooledConnectionLifetime`` attribute. This attribute defines how long connections remain active when pooled. Once this lifetime expires, the connection will no longer be pooled or issued for future requests.  

```csharp
[ApiController]
[Route("[controller]")]
public class ScenarioFourController : ControllerBase
{
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(20)
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

## Pros & cons from this scenario
### Pros 
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue. 
- It solves the DNS change issue mentioned on scenario 3.   
DNS resolution only occurs when a TCP connection is created, which means that if the DNS changes after the TCP connection has been created, then the TCP connection is unaware of it.   
The solution to avoid this issue is to create **short-lived** TCP connections that can be reused. Thus, when the time specified by the ``PooledConnectionLifetime`` property is reached, the TCP connection is closed, and a new one is created, forcing DNS resolution to occur again.

### Cons
- There are no disadvantages in this scenario.

# **Scenario 5: Use IHttpClientFactory**

## Source code

- An ``IHttpClientFactory`` named client is setup in the ``Program.cs`` _(this Scenario uses an ``IHttpClientFactory`` named client, you could use a typed client and the behaviour will be exactly the same)_.
- The ``SetHandlerLifetime`` extension method defines the length of time that a ``HttpMessageHandler`` instance can be reused before being discarded. It works almost identical as the ``PooledConnectionLifetime`` attribute from the previous scenario.
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
.SetHandlerLifetime(TimeSpan.FromMinutes(20));
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

## Pros & cons from this scenario
### Pros 
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue. 
- It solves the DNS change issue mentioned on scenario 3.
- It simplifies the declaration and usage of ``HttpClient`` instances.

### Cons
- The ``IHttpClientFactory`` keeps everything nice and simple as long as you only need to modify the common ``HttpClient`` parameters, it might be a bit harder if you need to tweak some of the less common parameters.     
The next code snippet is an example of how to set the ``PooledConnectionIdleTimeout`` attribute discussed on scenario 4.1, as you can see you'll need to use the ``ConfigurePrimaryHttpMessageHandler`` extension method and create a new ``SocketsHttpHandler`` instance, just to set the value of the ``PooledConnectionIdleTimeout`` attribute.

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

- This scenario uses Autofac as IoC container.
- An ``IHttpClientFactory`` named client is setup in the ``AutofacWebapiConfig.cs`` class.
- A few additional steps are required to make ``IHttpClientFactory`` work with Autofac:
    - Add required packages:
        - ``Microsoft.Extensions.Http``
    - ``IHttpClientFactory`` must be registered properly in Autofac IoC container. To do that, we must follow the next steps:
        - Create a new ``ServiceCollection`` instance.
        - Add the ``IHttpClientFactory`` named client.
        - Build the ``ServiceProvider`` and resolve ``IHttpClientFactory``.
        - **The ``IHttpClientFactory`` must be registered as a ``Singleton`` on Autofac**, or it won't work properly.

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
            .SetHandlerLifetime(TimeSpan.FromMinutes(20));

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

## Pros & cons from this scenario
### Pros 
- TCP connections are being reused, which further reduces the likelihood of experiencing a port exhaustion issue. 
- It solves the DNS change issues mentioned on scenario 3.

### Cons
- To avoid creating a new TCP connection every time a new request comes in, it is crucial to register the ``IHttpClientFactory`` as a Singleton in Autofac.
