using Xunit;

// QuizPackLoader.DataDirectory is process-wide, so tests that point it at the
// source data folder must not run alongside others.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
