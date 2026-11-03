using Xunit;

// Dice trzyma wspólny generator losowy, więc testy nie mogą biec równolegle
[assembly: CollectionBehavior(DisableTestParallelization = true)]
