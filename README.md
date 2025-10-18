# StockTrack - Distributed Real-Time Stock Price Tracking System

> A scalable distributed system demonstrating real-time stock price processing with Kafka, .NET 8, and Docker.

## Quick Start

### Prerequisites
- Docker Desktop 
- Docker Compose 

### Run the System
```bash
# Clone and navigate to directory
git clone https://github.com/jsdrolias/StockTrack.git
cd StockTrack

# Start all services (2 producers, 1 writer consumer, 1 metrics consumer)
docker-compose up -d

#or 

# Start services with multiple consumers(2 producers, 3 writer consumer, 2 metrics consumer)
docker-compose up --scale consumer-writer=3 --scale consumer-metrics=2

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```
   
## Architecture

The system comprises 3 major components:
1. Producer application instances that send messages regarding stock price changes
2. Consumer application instances that act upon receiving the stock price changes
3. The kafka broker that handles the communication between producers and consumers.

### Producers

Producer application instances send messages regarding stock price changes. Each producer is responsible for a single stock.

Configure via environment variables in `docker-compose.yml`:

| Variable               | Description                         | Example     |
|------------------------|-------------------------------------|-------------|
| STOCK_SIGN             | Stock ticker symbol                 | AAPL        |
| STARTING_PRICE         | Initial stock price                 | 242.65      |
| HEARTBEAT_MS           | Update frequency (ms)               | 100         |
| PROBABILITY_UP         | Probability of price increase (0-1) | 0.52        |
| MAX_CHANGE_PERCENT     | Maximum price change %              | 20          |

**Example: Adding a New Producer**

A new producer that tracks Microsoft (MSFT) stock can be added by editing docker compose file as follows:

```yaml
producer-msft:
  build:
    context: .
    dockerfile: StockTrackProducer/Dockerfile
  depends_on:
    kafka:
      condition: service_healthy
  environment:
    - STOCK_SIGN=MSFT
    - STARTING_PRICE=380.50
    - HEARTBEAT_MS=200
    - PROBABILITY_UP=0.55
    - MAX_CHANGE_PERCENT=10
  volumes:
    - ./output:/app/output
  networks:
    - kafka-network
```

### Kafka

Kafka's consumer group mechanism automatically distributes partitions across instances.

For the partitioning:

- **10 partitions** configured (can be adjusted in `docker-compose.yml`)
- **Partition key**: Stock symbol (ensures all updates for a stock go to same partition. **This ensures ordering of messages per stock**)
- **Load distribution**: Kafka balances partitions across consumer group members

### Consumers

The consumer application instances can be further subcategorized into 2 types:
1. Instances that receive the stock price changes and write them to a file
2. Instances that calculate the required metrics and write them into a file.
   Real-time calculation of:
  - Total messages received
  - Min/max price per stock (all-time)
  - High/low/open/close prices in sliding windows (for candlestick charts)

The 2 different consumer types use separate kafka consumer groups in order to ensure that all messages are consumed by both of them and are processed independently.

#### Consumer Writer Configuration

| Variable                | Description           | Default            |
|-------------------------|-----------------------|--------------------|
| CONSUMER_GROUP_ID       | Consumer group name   | stock-writer-group |
| BATCH_SIZE              | Messages per batch    | 50                |

The application writes the messages to disk in configurable batch sizes to reduce I/O operations.

#### Consumer Metrics Configuration

| Variable                | Description                              | Default             |
|-------------------------|------------------------------------------|---------------------|
| CONSUMER_GROUP_ID       | Consumer group name                      | stock-metrics-group |
| SLIDING_WINDOW_SECONDS  | Window duration for candlestick metrics  | 10                  |
| OUTPUT_INTERVAL_SECONDS | Interval for metrics output to file      | 6                   |

The application calculates the metrics and holds them in memory (it will grow indefinately and eventually crash the system but is acceptable for the demo).
A separate task writes the metrics to disk in a configurable periodic cycle.
It should be noted that the current sliding window will not be written to the file unless the window has expired.
Furthermore, multiple instances of the writer will write to respective files. A better approach would be a common shared store like Redis or a database.
This argument is further strenghted by the fact that as consumers are added/removed, consumer rebalancing will occur and some messages may be reprocessed by different instances, leading to inaccurate metrics and logs.

## Architecture Decisions

### Separate Consumer Applications vs Single App 

This implementation uses **two separate consumer applications** rather than multiple consumers in a single app.

**Benefits:**
- **Independent scaling**: Scale writers and metrics consumers separately based on their resource needs
- **Failure isolation**: If metrics calculation fails, file writing continues unaffected
- **Resource allocation**: Assign different CPU/memory per concern
- **Deployment flexibility**: Deploy, update, or rollback independently
- **Microservices alignment**: Better separation of concerns

**Alternative Approach:**

Multiple `BackgroundService` instances could be used in one application, sharing the same process. 
In addition, one application could have both consumer types and multiple consumer instances of each type.
This would be preferable when:
- Running in resource-constrained environments
- Both consumers have identical scaling needs
- Deployment simplicity is prioritized over operational flexibility
- Need to share in-memory state between consumers
- Lower operational complexity is more important than independent scaling

Note: This approach would require a different implementation for metrics calculations to ensure thread safety.

**Choice**: Multiple application instances with separate consumer apps. This approach demonstrates flexibility and a more robust distributed architecture suitable for production environments.

### Configuration Strategy

Application options are passed as environment variables via Docker Compose. App settings could be added as well to allow for more control in development/testing environments.

## Logging - Output files

All files are generated in the `./output/` directory:

**Logs** (Serilog structured logging):
- `stock-producer-logs-<guid>.txt` - Producer logs
- `stock-consumer-writer-logs-<guid>.txt` - Writer consumer logs
- `stock-consumer-metrics-logs-<guid>.txt` - Metrics consumer logs

Each log filename contains a random GUID per instance for correlation. Examples:
- `stock-producer-logs-e0513d61`
- `stock-consumer-writer-logs-a23385cb`
- `stock-consumer-metrics-logs-6f05ac01`

**Data Files**:
- `stock-data-<guid>.txt` - Raw stock price messages
- `metrics-<guid>.txt` - Calculated metrics (JSON format)

### Examples

**Metrics**
```json
{
  "totalMessages": 81,
  "stockSpecificMetrics": {
    "GOOGL": {
      "stockMinMaxPrice": {
        "minPrice": 41.9,
        "maxPrice": 145.81
      },
      "stockSlidingWindowMetrics": [
        {
          "openPrice": 145.81,
          "closePrice": 100.82,
          "highPrice": 145.81,
          "lowPrice": 62.83,
          "windowStart": "2025-10-17T12:55:09.2652887Z",
          "windowEnd": "2025-10-17T12:55:19.2884728Z"
        },
        {
          "openPrice": 96.83,
          "closePrice": 84.38,
          "highPrice": 107.5,
          "lowPrice": 41.9,
          "windowStart": "2025-10-17T12:55:19.2884728Z",
          "windowEnd": "2025-10-17T12:55:29.4890802Z"
        }
      ]
    }
  }
}
```

**Stock data**
```
2025-10-17 12:55:08.5524245 | AAPL | $251.77
2025-10-17 12:55:14.4309022 | AAPL | $232.82
2025-10-17 12:55:14.5512776 | AAPL | $209.73
2025-10-17 12:55:14.6737170 | AAPL | $228.77
2025-10-17 12:55:14.8276285 | AAPL | $184.79
2025-10-17 12:55:14.9446580 | AAPL | $221.45
2025-10-17 12:55:15.0756679 | AAPL | $216.69
2025-10-17 12:55:15.2026882 | AAPL | $174.39
2025-10-17 12:55:15.3326329 | AAPL | $148.24
2025-10-17 12:55:15.4551556 | AAPL | $152.99
```


## Unit tests

A test project has been added to the solution as an example.
Follows the same structure as the project it tests against, but contain the suffix "tests".
xUnit, FluentAssertions, NSubstitute packages have been used.

For the execution:
1. Open powershell
2. Navigate into the main folder of the solution
3. Execute ```dotnet test StockTrackProducerTests```

## Future Work

- Add integration tests. Can use Testcontainers for Kafka.
- Implement dead letter queue for failed messages
- Add retry policies with Polly
- Use a common shared store for metrics like Redis or a database