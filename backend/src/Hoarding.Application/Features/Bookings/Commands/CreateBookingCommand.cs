using FluentValidation;
using Hoarding.Application.Common.Interfaces;
using Hoarding.Application.Pricing;
using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;
using MediatR;

namespace Hoarding.Application.Features.Bookings.Commands;

public record CreateBookingCommand(
    Guid InventoryUnitId,
    Guid? CampaignId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? DeliverableSpecJson,
    decimal? DiscountPct,
    bool IncludeSetupCost = true
) : IRequest<CreateBookingResult>;

public record CreateBookingResult(
    Guid BookingId,
    string BookingRef,
    string Channel,
    decimal TotalAmount,
    string Currency,
    string Status
);

public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.InventoryUnitId).NotEmpty();
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Start date cannot be in the past.");
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.DiscountPct).InclusiveBetween(0, 50).When(x => x.DiscountPct.HasValue);
    }
}

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, CreateBookingResult>
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly PricingStrategyResolver _pricing;

    public CreateBookingCommandHandler(
        IBookingRepository bookingRepo,
        IInventoryRepository inventoryRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        PricingStrategyResolver pricing)
    {
        _bookingRepo = bookingRepo;
        _inventoryRepo = inventoryRepo;
        _currentUser = currentUser;
        _uow = uow;
        _pricing = pricing;
    }

    public async Task<CreateBookingResult> Handle(CreateBookingCommand req, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("User must be authenticated.");

        var unit = await _inventoryRepo.GetByIdAsync(req.InventoryUnitId, ct)
            ?? throw new KeyNotFoundException($"Inventory unit {req.InventoryUnitId} not found.");

        if (unit.Status != ListingStatus.Active)
            throw new InvalidOperationException("This inventory unit is not currently available for booking.");

        var hasConflict = await _bookingRepo.HasConflictingBookingAsync(
            req.InventoryUnitId, req.StartDate, req.EndDate, null, ct);
        if (hasConflict)
            throw new InvalidOperationException("Selected dates conflict with an existing booking.");

        var strategy = _pricing.For(unit.Channel);
        var basePrice = strategy.CalculateBasePrice(unit, req.StartDate, req.EndDate, req.DeliverableSpecJson);

        var setup = req.IncludeSetupCost ? unit.SetupCost : 0m;
        var discountPct = req.DiscountPct ?? 0m;
        var preDiscount = basePrice + setup;
        var discountAmount = Math.Round(preDiscount * discountPct / 100m, 2);
        var subtotal = preDiscount - discountAmount;
        var gst = Math.Round(subtotal * unit.GstPercentage / 100m, 2);
        var total = subtotal + gst;

        var booking = new Booking
        {
            BookingRef = GenerateBookingRef(),
            CampaignId = req.CampaignId,
            InventoryUnitId = unit.Id,
            Channel = unit.Channel,
            BookedBy = _currentUser.UserId,
            Status = BookingStatus.PendingPayment,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            DeliverableSpecJson = req.DeliverableSpecJson,
            BasePrice = basePrice,
            SetupCost = setup,
            DiscountPct = discountPct,
            DiscountAmount = discountAmount,
            Subtotal = subtotal,
            GstPercentage = unit.GstPercentage,
            GstAmount = gst,
            TotalAmount = total,
            Currency = unit.Currency,
            CreatedBy = _currentUser.UserId
        };

        await _bookingRepo.AddAsync(booking, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateBookingResult(
            booking.Id, booking.BookingRef, booking.Channel.ToString(),
            booking.TotalAmount, booking.Currency, booking.Status.ToString());
    }

    private static string GenerateBookingRef()
    {
        var year = DateTime.UtcNow.Year;
        var rand = Random.Shared.Next(100000, 999999);
        return $"BK-{year}-{rand:D6}";
    }
}
