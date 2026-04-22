using CommonResources;

using MassTransit;

using Microsoft.AspNetCore.Mvc;

namespace Producer.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueSenderController : ControllerBase
{
    private readonly IBus _bus;
    private readonly IRequestClient<TransferData> _client;

    public QueueSenderController(IBus bus, IRequestClient<TransferData> client)
    {
        _bus = bus;
        _client = client;
    }

    // ════════════════════════════════════════════════════════════════════════
    // RETRY POLICY демо
    // POST /QueueSender/send-command        → успешная обработка (Deposit=500)
    // POST /QueueSender/send-command-retry  → Deposit=-100, consumer бросит исключение → retry
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost("send-command")]
    public async Task<IActionResult> SendCommand()
    {
        var account = new Account { Name = "David Bytyqi", Deposit = 500 };
        var endpoint = await _bus.GetSendEndpoint(new Uri("rabbitmq://localhost/send-command"));
        await endpoint.Send(account);
        return Ok("Command sent — Deposit=500, should succeed.");
    }

    /// <summary>
    /// Отправляет аккаунт с отрицательным депозитом.
    /// Consumer бросит исключение → MassTransit выполнит 3 retry с интервалом 2 сек.
    /// Смотрите логи Consumer: [RETRY DEMO] Attempt #1, #2, #3...
    /// </summary>
    [HttpPost("send-command-retry")]
    public async Task<IActionResult> SendCommandRetry()
    {
        var account = new Account { Name = "David Bytyqi", Deposit = -100 };
        var endpoint = await _bus.GetSendEndpoint(new Uri("rabbitmq://localhost/send-command"));
        await endpoint.Send(account);
        return Ok("Command sent — Deposit=-100, consumer will throw → watch retry in Consumer logs.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // CIRCUIT BREAKER демо
    // POST /QueueSender/publish-event       → успешная обработка (Pin=123456)
    // POST /QueueSender/publish-event-fail  → Pin=0, каждый вызов считается ошибкой
    //                                         После 5+ ошибок circuit breaker "открывается"
    // ════════════════════════════════════════════════════════════════════════

    [HttpPost("publish-event")]
    public async Task<IActionResult> PublishEvent()
    {
        await _bus.Publish(new Client { Name = "David Bytyqi", Pin = 123456 });
        return Ok("Event published — Pin=123456, should succeed.");
    }

    /// <summary>
    /// Публикует событие с Pin=0 (симуляция недоступного сервиса).
    /// Отправьте 5+ раз подряд — circuit breaker откроется и начнёт отклонять сообщения.
    /// Смотрите логи Consumer: [CIRCUIT BREAKER DEMO]
    /// </summary>
    [HttpPost("publish-event-fail")]
    public async Task<IActionResult> PublishEventFail()
    {
        await _bus.Publish(new Client { Name = "David Bytyqi", Pin = 0 });
        return Ok("Event published — Pin=0, consumer will throw → watch circuit breaker in Consumer logs.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // OUTBOX демо
    // POST /QueueSender/request-response
    // Consumer использует UseInMemoryOutbox: response уйдёт только после
    // успешного завершения Consume-метода (не в середине транзакции).
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Request/Response через Outbox.
    /// Consumer обрабатывает TransferData и возвращает CurrentBalance.
    /// Response гарантированно отправляется только после успешного коммита (Outbox).
    /// </summary>
    [HttpPost("request-response")]
    public async Task<IActionResult> RequestResponse()
    {
        var requestData = new TransferData { Type = "Test", Amount = 25 };
        var request = _client.Create(requestData);
        var response = await request.GetResponse<CurrentBalance>();
        return Ok(response.Message);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SAGA демо
    // POST /QueueSender/start-saga           → запускает сагу (TransferRequested)
    // POST /QueueSender/complete-saga/{id}   → завершает сагу (TransferCompleted)
    // POST /QueueSender/fail-saga/{id}       → завершает сагу с ошибкой (TransferFailed)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Запускает новый экземпляр саги.
    /// Сага переходит из Initial → Pending.
    /// CorrelationId возвращается в ответе — используйте его для complete/fail.
    /// </summary>
    [HttpPost("start-saga")]
    public async Task<IActionResult> StartSaga()
    {
        var correlationId = NewId.NextSequentialGuid();
        await _bus.Publish(new TransferRequested
        {
            CorrelationId = correlationId,
            FromAccount = "ACC-001",
            ToAccount = "ACC-002",
            Amount = 250
        });
        return Ok(new { Message = "Saga started → state: Pending", CorrelationId = correlationId });
    }

    /// <summary>
    /// Завершает сагу успешно: Pending → Completed.
    /// Используйте CorrelationId из ответа start-saga.
    /// </summary>
    [HttpPost("complete-saga/{correlationId:guid}")]
    public async Task<IActionResult> CompleteSaga(Guid correlationId)
    {
        await _bus.Publish(new TransferCompleted
        {
            CorrelationId = correlationId,
            FinalBalance = 750
        });
        return Ok(new { Message = "TransferCompleted published → saga state: Completed", CorrelationId = correlationId });
    }

    /// <summary>
    /// Завершает сагу с ошибкой: Pending → Failed.
    /// Используйте CorrelationId из ответа start-saga.
    /// </summary>
    [HttpPost("fail-saga/{correlationId:guid}")]
    public async Task<IActionResult> FailSaga(Guid correlationId)
    {
        await _bus.Publish(new TransferFailed
        {
            CorrelationId = correlationId,
            Reason = "Insufficient funds (simulated)"
        });
        return Ok(new { Message = "TransferFailed published → saga state: Failed", CorrelationId = correlationId });
    }
}
