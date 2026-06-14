using FluentValidation;

namespace FleetIQ.Orders.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.WeightKg).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Pickup).NotNull();
        RuleFor(x => x.Delivery).NotNull();
    }
}
