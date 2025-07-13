using WorkerService1;

var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Azure Service Bus client
builder.AddAzureServiceBusClient("my-queue");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
