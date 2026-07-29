using FactoryGraph.Contracts;
using Highs;

namespace FactoryGraph.Solving;

internal sealed class HighsLinearProgramSolver : ILinearProgramSolver {
    public LinearProgramResult Solve(
        LinearProgram program,
        IReadOnlyList<double> costs,
        ModelRow? additionalRow,
        TimeSpan? timeLimit,
        NumericPolicy numericPolicy) {
        if (Enumerable.Range(0, program.VariableCount).Any(variable => program.LowerBounds[variable] > program.UpperBounds[variable])) {
            return new LinearProgramResult(LinearProgramStatus.Infeasible, [], "inconsistent variable bounds");
        }

        var rows = additionalRow is null ? program.Rows : [.. program.Rows, additionalRow];
        var starts = new int[rows.Count + 1];
        var indices = new List<int>();
        var values = new List<double>();
        var rowLower = new double[rows.Count];
        var rowUpper = new double[rows.Count];

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            starts[rowIndex] = indices.Count;
            rowLower[rowIndex] = rows[rowIndex].LowerBound;
            rowUpper[rowIndex] = rows[rowIndex].UpperBound;

            foreach (var (variable, coefficient) in rows[rowIndex].Coefficients.OrderBy(entry => entry.Key)) {
                indices.Add(variable);
                values.Add(coefficient);
            }
        }

        starts[^1] = indices.Count;
        using var solver = new HighsLpSolver();
        RequireOk(solver.setBoolOptionValue("output_flag", 0));
        var feasibilityTolerance = Math.Max(1e-10, numericPolicy.AbsoluteTolerance);
        RequireOk(solver.setDoubleOptionValue("primal_feasibility_tolerance", feasibilityTolerance));
        RequireOk(solver.setDoubleOptionValue("dual_feasibility_tolerance", feasibilityTolerance));

        if (timeLimit is { } duration) {
            RequireOk(solver.setDoubleOptionValue("time_limit", duration.TotalSeconds));
        }

        var nativeModel = new HighsModel(
            costs.ToArray(),
            program.LowerBounds.ToArray(),
            program.UpperBounds.ToArray(),
            rowLower,
            rowUpper,
            starts,
            indices.ToArray(),
            values.ToArray(),
            a_format: HighsMatrixFormat.kRowwise);

        RequireOk(solver.passLp(nativeModel));
        RequireOk(solver.run());

        var nativeStatus = solver.GetModelStatus();
        var solution = solver.getSolution().colvalue;

        return new LinearProgramResult(
            MapStatus(nativeStatus, program, rows, solution, numericPolicy),
            solution,
            nativeStatus.ToString());
    }

    private static LinearProgramStatus MapStatus(
        HighsModelStatus status,
        LinearProgram program,
        IReadOnlyList<ModelRow> rows,
        double[] values,
        NumericPolicy numericPolicy) {
        return status switch {
            HighsModelStatus.kOptimal => LinearProgramStatus.Optimal,
            HighsModelStatus.kInfeasible => LinearProgramStatus.Infeasible,
            HighsModelStatus.kUnbounded => LinearProgramStatus.Unbounded,
            HighsModelStatus.kTimeLimit or HighsModelStatus.kIterationLimit
                when IsFeasible(program, rows, values, numericPolicy) => LinearProgramStatus.Feasible,
            _ => LinearProgramStatus.Failed
        };
    }

    private static bool IsFeasible(
        LinearProgram program,
        IReadOnlyList<ModelRow> rows,
        double[] values,
        NumericPolicy numericPolicy) {
        if (values.Length != program.VariableCount || values.Any(value => !double.IsFinite(value))) {
            return false;
        }

        for (var variable = 0; variable < values.Length; variable++) {
            if (values[variable] < program.LowerBounds[variable]
                && !numericPolicy.NearlyEquals(values[variable], program.LowerBounds[variable])) {
                return false;
            }

            if (values[variable] > program.UpperBounds[variable]
                && !numericPolicy.NearlyEquals(values[variable], program.UpperBounds[variable])) {
                return false;
            }
        }

        foreach (var row in rows) {
            var value = row.Coefficients.Sum(entry => entry.Value * values[entry.Key]);

            if (value < row.LowerBound && !numericPolicy.NearlyEquals(value, row.LowerBound)) {
                return false;
            }

            if (value > row.UpperBound && !numericPolicy.NearlyEquals(value, row.UpperBound)) {
                return false;
            }
        }

        return true;
    }

    private static void RequireOk(HighsStatus status) {
        if (status != HighsStatus.kOk) {
            throw new InvalidOperationException($"HiGHS call failed with status '{status}'.");
        }
    }
}
