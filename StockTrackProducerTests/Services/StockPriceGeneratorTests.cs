using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StockTrackProducer.Services;
using Xunit;

namespace StockTrackProducerTests.Services;

public class StockPriceGeneratorTests
{
    private readonly ILogger<StockPriceGenerator> _logger;
    private readonly StockPriceGenerator _sut;

    public StockPriceGeneratorTests()
    {
        _logger = Substitute.For<ILogger<StockPriceGenerator>>();
        _sut = new StockPriceGenerator(_logger);
    }

    [Fact]
    public void GenerateNextPrice_ShouldReturnPositivePrice()
    {
        // Arrange
        var currentPrice = 100.0;
        var probabilityUp = 0.5;
        var maxChangePercent = 2;

        // Act
        var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);

        // Assert
        newPrice.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(100.0, 0.5, 2)]
    [InlineData(250.50, 0.6, 5)]
    [InlineData(50.25, 0.3, 1)]
    [InlineData(1000.0, 0.7, 10)]
    public void GenerateNextPrice_ShouldBeWithinExpectedRange(
        double currentPrice,
        double probabilityUp,
        int maxChangePercent)
    {
        // Arrange
        var expectedMinPrice = currentPrice * (1 - maxChangePercent / 100.0);
        var expectedMaxPrice = currentPrice * (1 + maxChangePercent / 100.0);

        // Act
        var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);

        // Assert
        newPrice.Should().BeInRange(expectedMinPrice, expectedMaxPrice);
    }

    [Theory]
    [InlineData(100.0, 1.0, 5)] // Always up
    [InlineData(100.0, 1.0, 10)] // Always up
    public void GenerateNextPrice_WithProbabilityOne_ShouldAlwaysIncrease(
        double currentPrice,
        double probabilityUp,
        int maxChangePercent)
    {
        // Arrange & Act
        var results = new List<double>();
        for (int i = 0; i < 100; i++)
        {
            var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);
            results.Add(newPrice);
        }

        // Assert
        results.Should().OnlyContain(price => price >= currentPrice);
    }

    [Theory]
    [InlineData(100.0, 0.0, 5)] // Always down
    [InlineData(100.0, 0.0, 10)] // Always down
    public void GenerateNextPrice_WithProbabilityZero_ShouldAlwaysDecreaseOrStaySame(
        double currentPrice,
        double probabilityUp,
        int maxChangePercent)
    {
        // Arrange & Act
        var results = new List<double>();
        for (int i = 0; i < 100; i++)
        {
            var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);
            results.Add(newPrice);
        }

        // Assert
        results.Should().OnlyContain(price => price <= currentPrice);
    }

    [Fact]
    public void GenerateNextPrice_WithZeroMaxChange_ShouldReturnSamePrice()
    {
        // Arrange
        var currentPrice = 100.0;
        var probabilityUp = 0.5;
        var maxChangePercent = 0;

        // Act
        var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);

        // Assert
        newPrice.Should().Be(currentPrice);
    }

    [Theory]
    [InlineData(242.65, 0.52, 2)]
    [InlineData(142.30, 0.48, 1)]
    public void GenerateNextPrice_ShouldProduceDifferentResults_OverMultipleCalls(
        double currentPrice,
        double probabilityUp,
        int maxChangePercent)
    {
        // Arrange & Act
        var results = new HashSet<double>();
        for (int i = 0; i < 50; i++)
        {
            var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);
            results.Add(newPrice);
        }

        // Assert
        results.Count.Should().BeGreaterThan(10,
            "because random generation should produce varied results");
    }

    [Fact]
    public void GenerateNextPrice_WithVerySmallPrice_ShouldHandleCorrectly()
    {
        // Arrange
        var currentPrice = 0.01;
        var probabilityUp = 0.5;
        var maxChangePercent = 2;

        // Act
        var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);

        // Assert
        newPrice.Should().BeGreaterThan(0);
        newPrice.Should().BeLessThanOrEqualTo(currentPrice * 1.02);
        newPrice.Should().BeGreaterThanOrEqualTo(currentPrice * 0.98);
    }

    [Fact]
    public void GenerateNextPrice_WithLargePrice_ShouldHandleCorrectly()
    {
        // Arrange
        var currentPrice = 10000.0;
        var probabilityUp = 0.5;
        var maxChangePercent = 2;

        // Act
        var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);

        // Assert
        newPrice.Should().BeGreaterThan(0);
        newPrice.Should().BeLessThanOrEqualTo(currentPrice * 1.02);
        newPrice.Should().BeGreaterThanOrEqualTo(currentPrice * 0.98);
    }

    [Fact]
    public void GenerateNextPrice_ShouldNotReturnNegativePrice_WithExtremSettings()
    {
        // Arrange
        var currentPrice = 1.0; // Small price
        var probabilityUp = 0.0; // Always decrease
        var maxChangePercent = 200; // Large change (200% down would be negative)

        // Act
        var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);

        // Assert
        newPrice.Should().BeGreaterThan(0);
        newPrice.Should().BeGreaterThanOrEqualTo(0.01);
    }

    [Fact]
    public void GenerateNextPrice_WithExtremeVolatility_ShouldEnforceMinimumPrice()
    {
        // Arrange
        var currentPrice = 0.50;
        var probabilityUp = 0.0; // Always decrease
        var maxChangePercent = 500; // Extreme change

        // Act
        var results = new List<double>();
        for (int i = 0; i < 10; i++)
        {
            var newPrice = _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);
            results.Add(newPrice);
        }

        // Assert
        results.Should().OnlyContain(price => price >= 0.01);
        results.Should().OnlyContain(price => price > 0);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StockPriceGenerator>>();

        // Act
        Action act = () => new StockPriceGenerator(logger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateNextPrice_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var currentPrice = 100.0;
        var probabilityUp = 0.5;
        var maxChangePercent = 2;

        // Act
        Action act = () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                _sut.GenerateNextPrice(currentPrice, probabilityUp, maxChangePercent);
            }
        };

        // Assert
        act.Should().NotThrow();
    }
}
