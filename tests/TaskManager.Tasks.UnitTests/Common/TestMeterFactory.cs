using System.Diagnostics.Metrics;

namespace TaskManager.Tasks.UnitTests.Common;

/// <summary>IMeterFactory mínimo: cada teste tem os próprios meters, sem vazar medições entre testes.</summary>
public sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in _meters)
            meter.Dispose();
        _meters.Clear();
    }
}
