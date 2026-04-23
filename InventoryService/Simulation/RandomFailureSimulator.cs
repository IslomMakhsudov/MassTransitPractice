namespace InventoryService.Simulation;

public class RandomFailureSimulator : IFailureSimulator
{
    private readonly double _failureRate;

    public RandomFailureSimulator(double failureRate = 0.3)
        => _failureRate = failureRate;

    public bool ShouldFail() => Random.Shared.NextDouble() < _failureRate;
}
