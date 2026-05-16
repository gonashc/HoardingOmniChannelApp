using Hoarding.Application.Common.Interfaces;
using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;
using System.Text.Json;

namespace Hoarding.Application.Pricing;

/// <summary>
/// Hoarding pricing: prefers monthly rate ≥30 days, weekly ≥7 days, daily otherwise.
/// </summary>
public class HoardingPricingStrategy : IPricingStrategy
{
    public ChannelType Channel => ChannelType.Hoarding;

    public decimal CalculateBasePrice(InventoryUnit unit, DateOnly start, DateOnly end, string? deliverableSpecJson)
    {
        var days = end.DayNumber - start.DayNumber + 1;
        if (days <= 0) throw new ArgumentException("End date must be after start date.");

        if (days >= 30 && unit.BasePriceMonthly.HasValue)
            return Math.Round(unit.BasePriceMonthly.Value * days / 30m, 2);
        if (days >= 7 && unit.BasePriceWeekly.HasValue)
            return Math.Round(unit.BasePriceWeekly.Value * days / 7m, 2);
        if (unit.BasePriceDaily.HasValue)
            return unit.BasePriceDaily.Value * days;
        if (unit.BasePriceMonthly.HasValue)
            return Math.Round(unit.BasePriceMonthly.Value * days / 30m, 2);

        throw new InvalidOperationException("Hoarding has no price configured.");
    }
}

/// <summary>
/// Influencer pricing: looks up per-deliverable rate from JSONB rate card.
/// Falls back to base_price_per_unit if not specified per deliverable type.
/// Deliverable spec format: {"deliverable":"reel","quantity":2}
/// </summary>
public class InfluencerPricingStrategy : IPricingStrategy
{
    public ChannelType Channel => ChannelType.Influencer;

    public decimal CalculateBasePrice(InventoryUnit unit, DateOnly start, DateOnly end, string? deliverableSpecJson)
    {
        if (unit.Influencer == null)
            throw new InvalidOperationException("Influencer spec missing.");

        // Default: 1 of base unit (e.g. 1 reel)
        var deliverable = unit.Influencer.AvailableDeliverables.FirstOrDefault().ToString().ToLowerInvariant();
        var quantity = 1;

        if (!string.IsNullOrWhiteSpace(deliverableSpecJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(deliverableSpecJson);
                if (doc.RootElement.TryGetProperty("deliverable", out var d))
                    deliverable = d.GetString()?.ToLowerInvariant() ?? deliverable;
                if (doc.RootElement.TryGetProperty("quantity", out var q))
                    quantity = q.GetInt32();
            }
            catch { /* malformed JSON falls back to defaults */ }
        }

        // Look up rate from per-deliverable pricing JSON
        var ratePerUnit = unit.BasePricePerUnit ?? 0m;
        if (!string.IsNullOrWhiteSpace(unit.Influencer.DeliverablePricingJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(unit.Influencer.DeliverablePricingJson);
                if (doc.RootElement.TryGetProperty(deliverable, out var rate))
                    ratePerUnit = rate.GetDecimal();
            }
            catch { }
        }

        return ratePerUnit * quantity;
    }
}

/// <summary>
/// Resolves the right strategy for a given channel.
/// </summary>
public class PricingStrategyResolver
{
    private readonly Dictionary<ChannelType, IPricingStrategy> _strategies;

    public PricingStrategyResolver(IEnumerable<IPricingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Channel);
    }

    public IPricingStrategy For(ChannelType channel)
    {
        if (!_strategies.TryGetValue(channel, out var s))
            throw new NotSupportedException($"No pricing strategy registered for channel {channel}.");
        return s;
    }
}
