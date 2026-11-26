using ShopSphere.BuildingBlocks.Application.Abstractions;
using ShopSphere.BuildingBlocks.Application.Messaging;
using ShopSphere.Contracts.Events;
using ShopSphere.Payment.Domain.Payments;

namespace ShopSphere.Payment.Application.Handlers;

public sealed class PaymentRequestedHandler : IIntegrationEventHandler<PaymentRequestedIntegrationEvent>
{
    private readonly IPaymentRepository _payments;
    private readonly IPaymentGateway _gateway;
    private readonly IUnitOfWork _uow;
    private readonly IIntegrationEventCollector _events;

    public PaymentRequestedHandler(IPaymentRepository payments, IPaymentGateway gateway, IUnitOfWork uow, IIntegrationEventCollector events)
    {
        _payments = payments; _gateway = gateway; _uow = uow; _events = events;
    }

    public async Task Handle(PaymentRequestedIntegrationEvent e, CancellationToken ct)
    {
        if (await _payments.GetByOrderAsync(e.OrderId, ct) is not null) return; // idempotent

        var payment = PaymentRecord.Create(e.OrderId, e.UserId, e.Amount, e.Currency);
        var result = await _gateway.ChargeAsync(e.OrderId, e.Amount, e.Currency, ct);

        if (result.Success)
        {
            payment.MarkSucceeded();
            _events.Collect(new PaymentSucceededIntegrationEvent(e.OrderId, payment.Id, e.Amount, e.Currency));
        }
        else
        {
            payment.MarkFailed(result.FailureReason ?? "Declined");
            _events.Collect(new PaymentFailedIntegrationEvent(e.OrderId, payment.FailureReason!));
        }

        await _payments.AddAsync(payment, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
