using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Billing.Domain;

public static class InvoiceErrors
{
    public static readonly Error NotFound = new("Invoice.NotFound", "The invoice was not found.");
    public static readonly Error CustomerRequired = new("Invoice.CustomerRequired", "A customer is required.");
    public static readonly Error InvalidCurrency = new("Invoice.InvalidCurrency", "Currency must be a 3-letter ISO code.");
    public static readonly Error NotDraft = new("Invoice.NotDraft", "Only a draft invoice can be modified.");
    public static readonly Error LineDescriptionRequired = new("Invoice.LineDescriptionRequired", "A line description is required.");
    public static readonly Error InvalidQuantity = new("Invoice.InvalidQuantity", "Quantity must be greater than zero.");
    public static readonly Error InvalidUnitPrice = new("Invoice.InvalidUnitPrice", "Unit price cannot be negative.");
    public static readonly Error CannotIssueEmpty = new("Invoice.CannotIssueEmpty", "An invoice with no positive total cannot be issued.");
    public static readonly Error NotIssued = new("Invoice.NotIssued", "Only an issued invoice can be paid.");
    public static readonly Error CannotCancelPaid = new("Invoice.CannotCancelPaid", "A paid invoice cannot be cancelled.");
    public static readonly Error AlreadyCancelled = new("Invoice.AlreadyCancelled", "The invoice is already cancelled.");
}
