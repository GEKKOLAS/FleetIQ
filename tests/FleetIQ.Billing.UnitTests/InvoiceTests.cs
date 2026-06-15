using FleetIQ.Billing.Domain;
using FleetIQ.Billing.Domain.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Billing.UnitTests;

public class InvoiceTests
{
    private static Invoice NewWithLine() =>
        Invoice.Create(Guid.NewGuid(), "EUR", new[] { ("Freight", 2, 100m) }).Value;

    [Fact]
    public void Create_WithLines_SetsTotalAndRaisesEvent()
    {
        var result = Invoice.Create(Guid.NewGuid(), "EUR", new[] { ("Freight", 2, 100m), ("Fee", 1, 50m) });

        result.IsSuccess.ShouldBeTrue();
        var invoice = result.Value;
        invoice.Status.ShouldBe(InvoiceStatus.Draft);
        invoice.Total.Amount.ShouldBe(250m);
        invoice.Total.Currency.ShouldBe("EUR");
        invoice.DomainEvents.OfType<InvoiceCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Create_WithBadCurrency_Fails()
    {
        var result = Invoice.Create(Guid.NewGuid(), "EURO", new[] { ("Freight", 1, 10m) });
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvoiceErrors.InvalidCurrency);
    }

    [Fact]
    public void AddLine_RecalculatesTotal()
    {
        var invoice = NewWithLine();          // 2 x 100 = 200
        invoice.AddLine("Extra", 3, 10m);     // + 30
        invoice.Total.Amount.ShouldBe(230m);
    }

    [Fact]
    public void Issue_EmptyInvoice_Fails()
    {
        // an invoice whose only line totals zero must not be issuable
        var draft = Invoice.Create(Guid.NewGuid(), "EUR", new[] { ("x", 1, 0m) }).Value;
        var result = draft.Issue(DateTime.UtcNow.AddDays(10));
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvoiceErrors.CannotIssueEmpty);
    }

    [Fact]
    public void Issue_ThenPay_FollowsLifecycle()
    {
        var invoice = NewWithLine();
        invoice.Issue(DateTime.UtcNow.AddDays(30)).IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Issued);

        invoice.RecordPayment().IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.DomainEvents.OfType<InvoicePaidDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Pay_DraftInvoice_Fails()
    {
        var invoice = NewWithLine();
        var result = invoice.RecordPayment();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvoiceErrors.NotIssued);
    }

    [Fact]
    public void AddLine_AfterIssue_Fails()
    {
        var invoice = NewWithLine();
        invoice.Issue(DateTime.UtcNow.AddDays(30));
        var result = invoice.AddLine("late", 1, 5m);
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvoiceErrors.NotDraft);
    }

    [Fact]
    public void Cancel_PaidInvoice_Fails()
    {
        var invoice = NewWithLine();
        invoice.Issue(DateTime.UtcNow.AddDays(30));
        invoice.RecordPayment();
        var result = invoice.Cancel();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvoiceErrors.CannotCancelPaid);
    }
}
