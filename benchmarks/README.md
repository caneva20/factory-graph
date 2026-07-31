# FactoryGraph benchmarks

The benchmark project measures each processor stage and the complete `FactoryGraphProcessor.Solve` call. It uses deterministic generated graphs so graph size changes without adding fixture-loading time to a measurement.

## Workloads

`AcyclicChain` connects each machine group to the next with one supply and one product outlet. `ConstrainedChain` uses the same topology and adds machine, connection, supply, and outlet capacities. `RecyclingRing` closes the final connection back to the first group, produces a coproduct at the first group, and includes a positive startup supply that remains unused at steady state. Each family runs in target-driven and capacity-driven modes with 8, 32, and 128 machine groups.

The `Family`, `Mode`, and `MachineGroups` columns identify each input. A chain with `n` machine groups has `n - 1` connections, while a recycling ring has `n`. Both shapes have `n + 1` items and `n` recipes.

## Measurements

| Benchmark | Timed work |
| --- | --- |
| `Validation` | Request validation |
| `TopologyAnalysis` | Strongly connected components and structural startup classification |
| `ModelConstruction` | Linear optimization-model construction |
| `BackendSolve` | One HiGHS invocation using a prebuilt model |
| `ResultConstruction` | Computed-graph projection and capacity bottleneck reporting |
| `EndToEnd` | Validation, model construction, all optimization passes, and result construction |

Benchmark setup generates and validates each request, prepares the model used by isolated stages, and confirms that both the model and complete processor return an optimal result. BenchmarkDotNet's memory diagnoser reports allocations for every measurement.

## Commands

List available benchmarks:

```bash
dotnet run -c Release --project benchmarks/FactoryGraph.Benchmarks -- --list flat
```

Run a short smoke measurement:

```bash
dotnet run -c Release --project benchmarks/FactoryGraph.Benchmarks -- --filter '*ProcessorStageBenchmarks*' --job Short
```

Run the baseline and export Markdown and CSV results:

```bash
dotnet run -c Release --project benchmarks/FactoryGraph.Benchmarks -- --filter '*ProcessorStageBenchmarks*' --exporters markdown csv
```

Measure the targeted 4,096-group capacity-driven workloads separately from the regular matrix:

```bash
dotnet run -c Release --project benchmarks/FactoryGraph.Benchmarks -- --filter '*LargeGraphBenchmarks*' --exporters markdown csv
```

Profile a representative end-to-end workload with BenchmarkDotNet's EventPipe profiler:

```bash
dotnet run -c Release --project benchmarks/FactoryGraph.Benchmarks -- --filter '*EndToEnd*' --profiler EP --job Short
```

Capture allocation stack samples with `dotnet-trace`:

```bash
dotnet-trace collect --profile gc-verbose -- \
  dotnet run -c Release --project benchmarks/FactoryGraph.Benchmarks --no-build -- \
  --filter '*ProcessorStageBenchmarks*' --job Short
```

Use the generated `.nettrace` files to identify methods with the largest CPU sample counts and allocation volumes. Report native HiGHS execution separately because managed stack samples attribute that time to the interop call.

## Baseline record

Run benchmarks on an otherwise idle machine using the repository's pinned SDK. Preserve the generated Markdown and CSV reports under `benchmarks/results/<yyyy-mm-dd>-<commit>/`. Record the following beside those files:

- Full commit SHA and whether the worktree was clean
- Benchmark command and BenchmarkDotNet job settings
- OS, CPU model, logical core count, and installed memory
- .NET SDK and runtime versions
- BenchmarkDotNet and `Highs.Native` package versions
- CPU and allocation hot methods found during profiling

BenchmarkDotNet writes runtime, OS, processor, and job details into its report. `global.json` pins the SDK; `Directory.Packages.props` records package versions. Do not compare results produced with different hardware or configuration as a regression series.
