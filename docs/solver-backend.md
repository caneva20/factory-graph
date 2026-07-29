# Solver Backend

FactoryGraph uses [HiGHS](https://highs.dev/) through the official `Highs.Native` package. HiGHS solves the [target-driven planning model](target-driven-planning.md). FactoryGraph configures its time limit and feasibility tolerances, then maps its model status and solution values to the public result contracts.

## Runtime Support

`Highs.Native` supplies native libraries for `win-x64`, `win-x86`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. Applications published for another runtime must supply a compatible HiGHS native library.
