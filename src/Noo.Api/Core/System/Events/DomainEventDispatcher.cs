using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Core.System.Events;

/// <summary>
/// Background pump that drains <see cref="DomainEventQueue"/> and fans events out to their
/// <see cref="IEventHandler{TEvent}"/> implementations.
///
/// Design notes:
/// - Handler invocation goes through pre-built strongly-typed delegates
///   (<see cref="DomainEventHandlerRegistry"/>) — no reflection on the hot path.
/// - Each handler runs in its own DI scope so a failing handler cannot poison another's
///   DbContext or transaction.
/// - Each handler is guarded by a configurable timeout — a stalled handler never blocks
///   the queue indefinitely.
/// - Events are processed with bounded concurrency so a burst cannot exhaust the DB pool.
/// - Exceptions are logged and isolated; the dispatcher itself never crashes on handler errors.
/// </summary>
public sealed class DomainEventDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DomainEventQueue _queue;
    private readonly DomainEventHandlerRegistry _registry;
    private readonly ILogger<DomainEventDispatcher> _logger;
    private readonly EventsConfig _options;

    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        DomainEventQueue queue,
        DomainEventHandlerRegistry registry,
        IOptions<EventsConfig> options,
        ILogger<DomainEventDispatcher> logger
    )
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _registry = registry;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var concurrency = new SemaphoreSlim(_options.MaxConcurrentEvents);
        var inFlight = new HashSet<Task>();

        try
        {
            await foreach (var @event in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await concurrency.WaitAsync(stoppingToken);

                var task = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await DispatchAsync(@event, stoppingToken);
                        }
                        finally
                        {
                            concurrency.Release();
                        }
                    },
                    stoppingToken
                );

                lock (inFlight)
                {
                    inFlight.Add(task);
                }

                _ = task.ContinueWith(
                    completed =>
                    {
                        lock (inFlight)
                        {
                            inFlight.Remove(completed);
                        }
                    },
                    TaskScheduler.Default
                );
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }

        Task[] outstanding;
        lock (inFlight)
        {
            outstanding = inFlight.ToArray();
        }

        if (outstanding.Length > 0)
        {
            try
            {
                await Task.WhenAll(outstanding).WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "Domain event dispatcher shutdown timed out with {Count} handler(s) still running.",
                    outstanding.Length
                );
            }
        }
    }

    private async Task DispatchAsync(IDomainEvent @event, CancellationToken stoppingToken)
    {
        var eventType = @event.GetType();

        if (!_registry.TryGet(eventType, out var handlerTypes, out var invoker))
        {
            return;
        }

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = stoppingToken,
            MaxDegreeOfParallelism = _options.MaxConcurrentHandlersPerEvent
        };

        await Parallel.ForEachAsync(
            handlerTypes,
            parallelOptions,
            (handlerType, ct) => InvokeHandlerAsync(handlerType, invoker, @event, ct)
        );
    }

    private async ValueTask InvokeHandlerAsync(
        Type handlerType,
        DomainEventHandlerRegistry.HandlerInvoker invoker,
        IDomainEvent @event,
        CancellationToken stoppingToken
    )
    {
        using var scope = _serviceProvider.CreateScope();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.HandlerTimeoutSeconds));

        try
        {
            var handler = scope.ServiceProvider.GetService(handlerType);

            if (handler is null)
            {
                _logger.LogError(
                    "Event handler {HandlerType} is in the handler map but cannot be resolved from DI.",
                    handlerType.FullName
                );
                return;
            }

            await invoker(handler, @event, timeoutCts.Token).ConfigureAwait(false);

            // Persist any tracked changes the handler made. Each handler owns its own scope and
            // therefore its own DbContext / UnitOfWork; the dispatcher commits so handlers stay
            // focused on business logic, mirroring the UnitOfWorkFilter used on HTTP requests.
            var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
            if (unitOfWork is not null)
            {
                await unitOfWork.CommitAsync(timeoutCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Event handler {HandlerType} timed out after {TimeoutSeconds}s while handling {EventType}.",
                handlerType.FullName,
                _options.HandlerTimeoutSeconds,
                @event.GetType().Name
            );
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown — swallow
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Event handler {HandlerType} threw while handling {EventType}.",
                handlerType.FullName,
                @event.GetType().Name
            );
        }
    }
}
