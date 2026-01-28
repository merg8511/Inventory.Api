using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Xunit;

namespace Inventory.Tests.Unit.Domain;

public class InventoryBalanceTests
{
    [Fact]
    public void AddStock_ShouldIncreaseOnHand()
    {
        // Arrange
        var balance = CreateBalance();
        var transactionId = Guid.NewGuid();
        
        // Act
        balance.AddStock(100, transactionId, DateTimeOffset.UtcNow);
        
        // Assert
        balance.OnHand.Should().Be(100);
        balance.Available.Should().Be(100);
        balance.LastTransactionId.Should().Be(transactionId);
    }
    
    [Fact]
    public void RemoveStock_ShouldDecreaseOnHand()
    {
        // Arrange
        var balance = CreateBalance(onHand: 100);
        
        // Act
        balance.RemoveStock(30, Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        // Assert
        balance.OnHand.Should().Be(70);
    }
    
    [Fact]
    public void RemoveStock_WhenInsufficientStock_ShouldThrow()
    {
        // Arrange
        var balance = CreateBalance(onHand: 10);
        
        // Act & Assert
        var act = () => balance.RemoveStock(20, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<InsufficientStockException>()
            .Which.RequestedQuantity.Should().Be(20);
    }
    
    [Fact]
    public void RemoveStock_WhenNegativeStockAllowed_ShouldNotThrow()
    {
        // Arrange
        var balance = CreateBalance(onHand: 10);
        
        // Act
        balance.RemoveStock(20, Guid.NewGuid(), DateTimeOffset.UtcNow, allowNegative: true);
        
        // Assert
        balance.OnHand.Should().Be(-10);
    }
    
    [Fact]
    public void Reserve_ShouldDecreaseAvailable()
    {
        // Arrange
        var balance = CreateBalance(onHand: 100);
        
        // Act
        balance.Reserve(30);
        
        // Assert
        balance.OnHand.Should().Be(100);
        balance.Reserved.Should().Be(30);
        balance.Available.Should().Be(70);
    }
    
    [Fact]
    public void Unreserve_ShouldIncreaseAvailable()
    {
        // Arrange
        var balance = CreateBalance(onHand: 100, reserved: 30);
        
        // Act
        balance.Unreserve(20);
        
        // Assert
        balance.Reserved.Should().Be(10);
        balance.Available.Should().Be(90);
    }
    
    [Fact]
    public void AddInTransit_ShouldIncreaseInTransit()
    {
        // Arrange
        var balance = CreateBalance();
        
        // Act
        balance.AddInTransit(50);
        
        // Assert
        balance.InTransit.Should().Be(50);
    }
    
    [Fact]
    public void RemoveInTransit_ShouldDecreaseInTransit()
    {
        // Arrange
        var balance = CreateBalance(inTransit: 50);
        
        // Act
        balance.RemoveInTransit(30);
        
        // Assert
        balance.InTransit.Should().Be(20);
    }
    
    private static InventoryBalance CreateBalance(
        decimal onHand = 0, 
        decimal reserved = 0, 
        decimal inTransit = 0)
    {
        return new InventoryBalance
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            OnHand = onHand,
            Reserved = reserved,
            InTransit = inTransit,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
