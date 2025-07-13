using Azure.Messaging.ServiceBus;

namespace WorkerService1;

public class Worker(ILogger<Worker> logger, ServiceBusClient serviceBusClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create a processor for receiving messages
        await using var processor = serviceBusClient.CreateProcessor("my-queue");
        
        // Set up message handling
        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;
        // Start processing
        await processor.StartProcessingAsync(stoppingToken);

        await Task.Delay(-1, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        
        // Stop processing and dispose
        await processor.StopProcessingAsync(stoppingToken);
    }
    
    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        logger.LogInformation("Received message: {message}", args.Message.Body.ToString());
        
        // Complete the message to remove it from the queue
        await args.CompleteMessageAsync(args.Message);
    }
    
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Error processing message");
        return Task.CompletedTask;
    }
}
