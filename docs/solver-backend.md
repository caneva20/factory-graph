# Solver Backend

FactoryGraph uses [HiGHS](https://highs.dev/) through the official `Highs.Native` package. HiGHS solves the [target-driven planning model](target-driven-planning.md). FactoryGraph configures its time limit and feasibility tolerances, then maps its model status and solution values to the public result contracts.

## License and Maintenance

HiGHS and `Highs.Native` use the MIT license. FactoryGraph depends on the official package so backend upgrades and supported native runtimes follow the HiGHS release lifecycle.

## Sparse Model

FactoryGraph stores each constraint row by nonzero variable coefficient. The backend adapter passes those rows to HiGHS as a row-wise sparse matrix instead of allocating a dense constraint matrix.

## Performance

Target-driven planning performs one native linear-program solve for the selected objective and, when needed, a second solve to minimize total machine activity. Both solves share the request time limit. Model construction and result projection run in managed code.

FactoryGraph does not define a solve-time guarantee. [Benchmark issue #34](https://github.com/caneva20/factory-planner/issues/34) will establish representative graph sizes, measurements, and performance budgets.

## Runtime Support

`Highs.Native` supplies native libraries for `win-x64`, `win-x86`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. Applications published for another runtime must supply a compatible HiGHS native library.
