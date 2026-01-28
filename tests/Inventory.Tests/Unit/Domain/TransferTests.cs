using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Xunit;

namespace Inventory.Tests.Unit.Domain;

public class TransferTests
{
    [Fact]
    public void NewTransfer_ShouldHaveDraftStatus()
    {
        // Arrange & Act
        var transfer = CreateTransfer();
        
        // Assert
        transfer.Status.Should().Be(TransferStatus.Draft);
    }
    
    [Fact]
    public void Commit_FromDraft_ShouldChangeToCommitted()
    {
        // Arrange
        var transfer = CreateTransferWithLines();
        
        // Act
        transfer.Commit(DateTimeOffset.UtcNow);
        
        // Assert
        transfer.Status.Should().Be(TransferStatus.Committed);
        transfer.CommittedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Commit_WithNoLines_ShouldThrow()
    {
        // Arrange
        var transfer = CreateTransfer();
        
        // Act & Assert
        var act = () => transfer.Commit(DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>()
            .WithMessage("*at least one line*");
    }
    
    [Fact]
    public void Ship_FromCommitted_ShouldChangeToInTransit()
    {
        // Arrange
        var transfer = CreateTransferWithLines();
        transfer.Commit(DateTimeOffset.UtcNow);
        
        // Act
        transfer.Ship(DateTimeOffset.UtcNow);
        
        // Assert
        transfer.Status.Should().Be(TransferStatus.InTransit);
        transfer.ShippedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Ship_FromDraft_ShouldThrow()
    {
        // Arrange
        var transfer = CreateTransfer();
        
        // Act & Assert
        var act = () => transfer.Ship(DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>()
            .WithMessage("*Must be Committed*");
    }
    
    [Fact]
    public void Receive_FromInTransit_ShouldChangeToReceived()
    {
        // Arrange
        var transfer = CreateTransferWithLines();
        transfer.Commit(DateTimeOffset.UtcNow);
        transfer.Ship(DateTimeOffset.UtcNow);
        
        // Act
        transfer.Receive(DateTimeOffset.UtcNow);
        
        // Assert
        transfer.Status.Should().Be(TransferStatus.Received);
        transfer.ReceivedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Cancel_FromDraft_ShouldChangeToCancelled()
    {
        // Arrange
        var transfer = CreateTransfer();
        
        // Act
        transfer.Cancel(DateTimeOffset.UtcNow);
        
        // Assert
        transfer.Status.Should().Be(TransferStatus.Cancelled);
        transfer.CancelledAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Cancel_FromReceived_ShouldThrow()
    {
        // Arrange
        var transfer = CreateTransferWithLines();
        transfer.Commit(DateTimeOffset.UtcNow);
        transfer.Ship(DateTimeOffset.UtcNow);
        transfer.Receive(DateTimeOffset.UtcNow);
        
        // Act & Assert
        var act = () => transfer.Cancel(DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot cancel*");
    }
    
    private static Transfer CreateTransfer()
    {
        return new Transfer
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TransferNumber = "TRF-20260128-0001",
            SourceWarehouseId = Guid.NewGuid(),
            DestinationWarehouseId = Guid.NewGuid(),
            Status = TransferStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user"
        };
    }
    
    private static Transfer CreateTransferWithLines()
    {
        var transfer = CreateTransfer();
        transfer.Lines.Add(new TransferLine
        {
            Id = Guid.NewGuid(),
            TransferId = transfer.Id,
            ItemId = Guid.NewGuid(),
            RequestedQuantity = 10
        });
        return transfer;
    }
}
