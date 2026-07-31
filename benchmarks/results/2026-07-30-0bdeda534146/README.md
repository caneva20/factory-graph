# Recycling-Cycle Benchmark Review

## Run

- Commit: `0bdeda5341461271724e100c2e3f2bf1543c2fd4`
- Worktree: dirty with the issue #3 implementation under measurement
- Command: `dotnet run --configuration Release --project benchmarks/FactoryGraph.Benchmarks --no-build -- --filter '*ProcessorStageBenchmarks*' --job Short --exporters markdown csv --artifacts "benchmarks/results/2026-07-30-0bdeda534146/artifacts"`
- Job: ShortRun, one launch, three warmups, three measured iterations
- OS: CachyOS Linux, kernel `7.1.3-2-cachyos`, x86-64
- CPU: AMD Ryzen 5 5600G, 6 physical cores, 12 logical cores
- Memory: 31 GiB
- SDK: .NET SDK 10.0.110
- Runtime: .NET 10.0.10, x64 RyuJIT x86-64-v3
- BenchmarkDotNet: 0.15.8
- Highs.Native: 1.15.1

The generated Markdown, CSV, HTML, and log files are under `artifacts/`.

## Review

At 128 groups, target-driven `RecyclingRing` end-to-end time was 1,294 us, compared with 1,373 us for `AcyclicChain` and 1,408 us for `ConstrainedChain`. Capacity-driven `RecyclingRing` took 1,368 us, compared with 1,246 us and 1,368 us. Recycling allocations were 692 KB in target-driven mode and 752 KB in capacity-driven mode, matching the corresponding acyclic workloads within 1 KB.

The isolated topology analysis grew from about 5 us at 8 groups to 61 us at 128 groups. At 128 groups, acyclic and constrained analysis took 57 to 60 us. Recycling analysis allocated 153 KB, compared with 155 KB for both chain families.

The cyclic backend solve was 5% to 8% slower than the chain backends at 128 groups. End-to-end results showed no consistent cycle-specific regression, superlinear growth, or allocation increase. The ShortRun confidence intervals are broad, so these files establish a local comparison rather than a regression threshold.

No profiler run or follow-up issue was needed because the comparison found no cycle-specific hotspot. Run the full default job before setting performance budgets or treating future differences as regressions.
