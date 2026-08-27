using Xunit;

// Every test in this assembly shells out to `dotnet build` - and three of them to `dotnet run` -
// on a generated project in a temp directory. Those child processes are themselves parallel, and
// they share one NuGet cache, one MSBuild node pool and one SDK install, so running many at once
// makes them fight rather than go faster: MSB3026 (copy retry limit exceeded) and MSB4018
// (GenerateDepsFile failed) start appearing, and which tests they hit is random.
//
// Two at a time keeps the machine busy without the contention. This is not a guess - the suite
// was intermittently red at full parallelism, and 27 of 50 failed in one run with 175 MSBuild
// contention errors in the log.
[assembly: CollectionBehavior(MaxParallelThreads = 2)]
