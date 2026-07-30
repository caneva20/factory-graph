using FactoryGraph.Contracts;
using Highs;

namespace FactoryGraph.Solving;

internal sealed class HighsLinearProgramSolver : ILinearProgramSolver {
    public ILinearProgramSession CreateSession(
        LinearProgram program,
        NumericPolicy numericPolicy) {
        for (var variable = 0; variable < program.VariableCount; variable++) {
            if (program.LowerBounds[variable] > program.UpperBounds[variable]) {
                return InfeasibleSession.Instance;
            }
        }

        return new Session(program, numericPolicy);
    }

    private sealed class Session : ILinearProgramSession {
        private readonly LinearProgram _program;
        private readonly NumericPolicy _numericPolicy;
        private readonly HighsLpSolver _solver = new();
        private bool _modelLoaded;
        private ModelRow? _additionalRow;
        private int[]? _variableIndices;

        public Session(LinearProgram program, NumericPolicy numericPolicy) {
            _program = program;
            _numericPolicy = numericPolicy;

            RequireOk(_solver.setBoolOptionValue("output_flag", 0));
            var feasibilityTolerance = Math.Max(1e-10, numericPolicy.AbsoluteTolerance);
            RequireOk(_solver.setDoubleOptionValue("primal_feasibility_tolerance", feasibilityTolerance));
            RequireOk(_solver.setDoubleOptionValue("dual_feasibility_tolerance", feasibilityTolerance));
        }

        public LinearProgramResult Solve(IReadOnlyList<double> costs, TimeSpan? timeLimit) {
            Prepare(costs, null);
            return Run(timeLimit);
        }

        public LinearProgramResult AddRowAndSolve(
            IReadOnlyList<double> costs,
            ModelRow additionalRow,
            TimeSpan? timeLimit) {
            if (_additionalRow is not null) {
                throw new InvalidOperationException("A linear-program session supports one additional row.");
            }

            Prepare(costs, additionalRow);
            _additionalRow = additionalRow;
            return Run(timeLimit);
        }

        public void Dispose() {
            _solver.Dispose();
        }

        private void Prepare(IReadOnlyList<double> costs, ModelRow? additionalRow) {
            if (costs.Count != _program.VariableCount) {
                throw new ArgumentException("The cost count must match the linear program's variable count.", nameof(costs));
            }

            if (!_modelLoaded) {
                LoadModel(costs, additionalRow);
                return;
            }

            RequireOk(_solver.clearSolver());
            _variableIndices ??= CreateVariableIndices(_program.VariableCount);
            RequireOk(_solver.changeColsCostBySet(_variableIndices, AsArray(costs)));

            if (additionalRow is not null) {
                var indices = new int[additionalRow.Coefficients.Count];
                var values = new double[additionalRow.Coefficients.Count];
                FillCoefficients(additionalRow, indices, values, 0);
                RequireOk(_solver.addRow(additionalRow.LowerBound, additionalRow.UpperBound, indices, values));
            }
        }

        private void LoadModel(IReadOnlyList<double> costs, ModelRow? additionalRow) {
            var rowCount = _program.Rows.Count + (additionalRow is null ? 0 : 1);
            var coefficientCount = additionalRow?.Coefficients.Count ?? 0;

            foreach (var row in _program.Rows) {
                coefficientCount += row.Coefficients.Count;
            }

            var starts = new int[rowCount + 1];
            var indices = new int[coefficientCount];
            var values = new double[coefficientCount];
            var rowLower = new double[rowCount];
            var rowUpper = new double[rowCount];
            var coefficientIndex = 0;

            for (var rowIndex = 0; rowIndex < _program.Rows.Count; rowIndex++) {
                coefficientIndex = FillRow(
                    _program.Rows[rowIndex],
                    rowIndex,
                    coefficientIndex,
                    starts,
                    indices,
                    values,
                    rowLower,
                    rowUpper);
            }

            if (additionalRow is not null) {
                coefficientIndex = FillRow(
                    additionalRow,
                    _program.Rows.Count,
                    coefficientIndex,
                    starts,
                    indices,
                    values,
                    rowLower,
                    rowUpper);
            }

            starts[^1] = coefficientIndex;
            var nativeModel = new HighsModel(
                AsArray(costs),
                AsArray(_program.LowerBounds),
                AsArray(_program.UpperBounds),
                rowLower,
                rowUpper,
                starts,
                indices,
                values,
                a_format: HighsMatrixFormat.kRowwise);

            RequireOk(_solver.passLp(nativeModel));
            _modelLoaded = true;
        }

        private LinearProgramResult Run(TimeSpan? timeLimit) {
            if (timeLimit is { } duration) {
                RequireOk(_solver.setDoubleOptionValue("time_limit", duration.TotalSeconds));
            }

            RequireOk(_solver.run());
            var nativeStatus = _solver.GetModelStatus();
            var solution = _solver.getSolution().colvalue;

            return new LinearProgramResult(
                MapStatus(nativeStatus, _program, _additionalRow, solution, _numericPolicy),
                solution,
                nativeStatus.ToString());
        }

        private static int FillRow(
            ModelRow row,
            int rowIndex,
            int coefficientIndex,
            int[] starts,
            int[] indices,
            double[] values,
            double[] rowLower,
            double[] rowUpper) {
            starts[rowIndex] = coefficientIndex;
            rowLower[rowIndex] = row.LowerBound;
            rowUpper[rowIndex] = row.UpperBound;

            return FillCoefficients(row, indices, values, coefficientIndex);
        }

        private static int FillCoefficients(ModelRow row, int[] indices, double[] values, int coefficientIndex) {
            foreach (var (variable, coefficient) in row.Coefficients) {
                indices[coefficientIndex] = variable;
                values[coefficientIndex] = coefficient;
                coefficientIndex++;
            }

            return coefficientIndex;
        }

        private static int[] CreateVariableIndices(int count) {
            var indices = new int[count];
            for (var index = 0; index < count; index++) indices[index] = index;

            return indices;
        }

        private static double[] AsArray(IReadOnlyList<double> values) {
            if (values is double[] array) return array;

            var copy = new double[values.Count];
            for (var index = 0; index < values.Count; index++) copy[index] = values[index];

            return copy;
        }
    }

    private static LinearProgramStatus MapStatus(
        HighsModelStatus status,
        LinearProgram program,
        ModelRow? additionalRow,
        double[] values,
        NumericPolicy numericPolicy) {
        return status switch {
            HighsModelStatus.kOptimal => LinearProgramStatus.Optimal,
            HighsModelStatus.kInfeasible => LinearProgramStatus.Infeasible,
            HighsModelStatus.kUnbounded => LinearProgramStatus.Unbounded,
            HighsModelStatus.kTimeLimit or HighsModelStatus.kIterationLimit
                when IsFeasible(program, additionalRow, values, numericPolicy) => LinearProgramStatus.Feasible,
            _ => LinearProgramStatus.Failed
        };
    }

    private static bool IsFeasible(
        LinearProgram program,
        ModelRow? additionalRow,
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

        foreach (var row in program.Rows) {
            if (!IsRowFeasible(row, values, numericPolicy)) return false;
        }

        return additionalRow is null || IsRowFeasible(additionalRow, values, numericPolicy);
    }

    private static bool IsRowFeasible(ModelRow row, double[] values, NumericPolicy numericPolicy) {
        var value = 0d;
        foreach (var (variable, coefficient) in row.Coefficients) value += coefficient * values[variable];

        return (value >= row.LowerBound || numericPolicy.NearlyEquals(value, row.LowerBound))
               && (value <= row.UpperBound || numericPolicy.NearlyEquals(value, row.UpperBound));
    }

    private static void RequireOk(HighsStatus status) {
        if (status != HighsStatus.kOk) {
            throw new InvalidOperationException($"HiGHS call failed with status '{status}'.");
        }
    }

    private sealed class InfeasibleSession : ILinearProgramSession {
        public static readonly InfeasibleSession Instance = new();

        public LinearProgramResult Solve(IReadOnlyList<double> costs, TimeSpan? timeLimit) => Result();

        public LinearProgramResult AddRowAndSolve(
            IReadOnlyList<double> costs,
            ModelRow additionalRow,
            TimeSpan? timeLimit) => Result();

        public void Dispose() { }

        private static LinearProgramResult Result() =>
            new(LinearProgramStatus.Infeasible, [], "inconsistent variable bounds");
    }
}
