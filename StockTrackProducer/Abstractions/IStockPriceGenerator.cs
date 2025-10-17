namespace StockTrackProducer.Abstractions;

public interface IStockPriceGenerator
{
    double GenerateNextPrice(double currentPrice, double probabilityUp, int maxChangePercent);
}
