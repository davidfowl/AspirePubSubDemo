var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Service Bus
var serviceBus = builder.AddAzureServiceBus("servicebus")
                        .RunAsEmulator(e => e.WithLifetime(ContainerLifetime.Persistent))
                        .WithCommands();

var serviceBusQueue = serviceBus.AddServiceBusQueue("my-queue");
serviceBus.AddServiceBusQueue("dead-letter-queue");

// Add the worker service and reference the service bus
builder.AddProject<Projects.WorkerService1>("workerservice1")
    .WithReference(serviceBusQueue)
    .WaitFor(serviceBusQueue);

builder.Build().Run();
