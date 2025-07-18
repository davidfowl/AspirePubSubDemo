using System.Reflection;
using Aspire.Hosting.Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public static class ServiceBusExtensions
{
    public static IResourceBuilder<AzureServiceBusResource> WithCommands(this IResourceBuilder<AzureServiceBusResource> builder)
    {
        // This property isn't public, so we use reflection to access it
        // See https://github.com/dotnet/aspire/issues/10367
        var queuesProperty = typeof(AzureServiceBusResource).GetProperty("Queues", BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new InvalidOperationException("Could not find 'Queues' property on AzureServiceBusResource.");

        return builder.WithCommand("publish", "Publish Message", async (context) =>
        {
            var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();

            var queues = (queuesProperty.GetValue(builder.Resource) as List<AzureServiceBusQueueResource> ?? [])
                        .ToDictionary(q => q.Name);

            // Get the connection string from the queue resource
            var inputs = new InteractionInput[]
            {
                new() {
                    InputType = InputType.Choice,
                    Label = "Queue",
                    Options = [.. queues.Select(q => KeyValuePair.Create(q.Key, q.Value.QueueName))],
                    Value = queues.FirstOrDefault().Value.QueueName
                },
                new() {
                    InputType = InputType.Text,
                    Label = "Message"
                }
            };

            var result = await interactionService.PromptInputsAsync(
                "Enter message to publish",
                $"Send a message to queue",
                inputs,
                new InputsDialogInteractionOptions
                {
                    PrimaryButtonText = "Send",
                },
                context.CancellationToken);

            if (result.Canceled)
            {
                return new ExecuteCommandResult
                {
                    Success = true,
                    ErrorMessage = "Operation cancelled by user."
                };
            }

            try
            {
                var queueResource = queues[result.Data[0].Value!];
                var connectionString = await queueResource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);

                await using var serviceBusClient = new ServiceBusClient(connectionString);
                await using var sender = serviceBusClient.CreateSender(queueResource.QueueName);

                var message = new ServiceBusMessage(result.Data[1].Value);
                await sender.SendMessageAsync(message, context.CancellationToken);

                return new ExecuteCommandResult
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ExecuteCommandResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to publish message: {ex.Message}"
                };
            }
        }, new()
        {
            IsHighlighted = true,
            IconName = "Send",
            IconVariant = IconVariant.Filled
        });
    }
}
