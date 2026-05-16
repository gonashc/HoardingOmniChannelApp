using Hoarding.Application.Common.Interfaces;
using Hoarding.Application.Features.Inventory.DTOs;
using Hoarding.Application.Pricing;
using MediatR;

namespace Hoarding.Application.Features.Inventory.Queries;

public record GetInstantQuoteQuery(
    Guid InventoryUnitId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? DeliverableSpecJson = null,
    bool IncludeSetupCost = true
) : IRequest<QuoteResponseDto?>;

public class GetInstantQuoteQueryHandler : IRequestHandler<GetInstantQuoteQuery, QuoteResponseDto?>
{
    private readonly IInventoryRepository _repo;
    private readonly PricingStrategyResolver _pricing;

    public GetInstantQuoteQueryHandler(IInventoryRepository repo, PricingStrategyResolver pricing)
    {
        _repo = repo;
        _pricing = pricing;
    }

    public async Task<QuoteResponseDto?> Handle(GetInstantQuoteQuery req, CancellationToken ct)
    {
        var u = await _repo.GetByIdAsync(req.InventoryUnitId, ct);
        if (u == null) return null;

        var days = req.EndDate.DayNumber - req.StartDate.DayNumber + 1;
        if (days <= 0) throw new ArgumentException("End date must be after start date.");

        var strategy = _pricing.For(u.Channel);
        var baseAmount = strategy.CalculateBasePrice(u, req.StartDate, req.EndDate, req.DeliverableSpecJson);

        var setup = req.IncludeSetupCost ? u.SetupCost : 0m;
        var subtotal = baseAmount + setup;
        var gst = Math.Round(subtotal * u.GstPercentage / 100m, 2);
        var total = subtotal + gst;

        var breakdown = u.Channel switch
        {
            Domain.Enums.ChannelType.Hoarding =>
                $"{days} day(s) at {u.Currency}{u.BasePriceMonthly:N0}/month equivalent",
            Domain.Enums.ChannelType.Influencer =>
                $"Per the deliverable rate card",
            _ => $"{days} day(s)"
        };

        return new QuoteResponseDto(
            u.Id, u.Channel.ToString(),
            req.StartDate, req.EndDate, days,
            baseAmount, setup, subtotal, gst, total,
            u.Currency, breakdown);
    }
}
