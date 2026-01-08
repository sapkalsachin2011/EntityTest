using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EntityTestApi.Kafka
{
    public class KafkaProducerService
    {
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            _topic = configuration["Kafka:Topic"] ?? "suppliers";
            _logger = logger;
        }

        public async Task ProduceAsync(string message)
        {
            var config = new ProducerConfig {
                BootstrapServers = _bootstrapServers,
                ReceiveMessageMaxBytes = 100000000, // 100MB, adjust as needed
                MessageTimeoutMs = 20000 // 10 seconds, adjust as needed
            };
            try
            {
                _logger.LogInformation($"Producing Kafka message to topic '{_topic}': {message}");
                using var producer = new ProducerBuilder<Null, string>(config).Build();
                _logger.LogInformation("Awaiting Kafka delivery...");
                var deliveryResult = await producer.ProduceAsync(_topic, new Message<Null, string> { Value = message });
                if (deliveryResult != null)
                {
                    _logger.LogInformation($"Kafka message delivered to {deliveryResult.TopicPartitionOffset} (status: {deliveryResult.Status})");
                }
                else
                {
                    _logger.LogWarning("Kafka deliveryResult was null after ProduceAsync.");
                }
                _logger.LogInformation("Code after deliveryResult log reached.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error producing Kafka message: {ex.Message}");
                throw;
            }
        }
    }
}
