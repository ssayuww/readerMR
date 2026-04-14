using serverreader.Models;
using serverreader.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DeviceStore>();

var app = builder.Build();

app.MapGet("/", () => "Server is running");

app.MapPost("/api/devices/update", (DeviceSnapshot snapshot, DeviceStore store) =>
{
    if (string.IsNullOrWhiteSpace(snapshot.DeviceId))
        return Results.BadRequest("DeviceId is required");

    store.Save(snapshot);
    return Results.Ok(new { message = "Snapshot received" });
});

app.MapGet("/api/devices", (DeviceStore store) =>
{
    return Results.Ok(store.GetAll());
});

app.MapGet("/api/devices/{deviceId}", (string deviceId, DeviceStore store) =>
{
    var device = store.GetById(deviceId);

    if (device is null)
        return Results.NotFound();

    return Results.Ok(device);
});

app.Run("http://0.0.0.0:5000");