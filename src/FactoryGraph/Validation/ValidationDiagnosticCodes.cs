// ReSharper disable MemberHidesStaticFromOuterClass

namespace FactoryGraph.Validation;

/// <summary>
///     Stable diagnostic codes reported when a solve request fails validation.
/// </summary>
public static class ValidationDiagnosticCodes {
    private const string Path = "validation";

    /// <summary>Diagnostic codes for element and reference IDs.</summary>
    public static class Id {
        private const string Path = ValidationDiagnosticCodes.Path + ".id";

        /// <summary>An element or reference ID is absent.</summary>
        public const string Required = Path + ".required";

        /// <summary>An element ID occurs more than once within its element kind.</summary>
        public const string Duplicate = Path + ".duplicate";
    }

    /// <summary>Diagnostic codes for graph references.</summary>
    public static class Reference {
        private const string Path = ValidationDiagnosticCodes.Path + ".reference";

        /// <summary>A referenced catalog or graph element does not exist.</summary>
        public const string NotFound = Path + ".not-found";
    }

    /// <summary>Diagnostic codes for item compatibility.</summary>
    public static class Item {
        private const string Path = ValidationDiagnosticCodes.Path + ".item";

        /// <summary>A connection or boundary uses an item incompatible with its machine recipe.</summary>
        public const string Incompatible = Path + ".incompatible";
    }

    /// <summary>Diagnostic codes for planning modes.</summary>
    public static class Mode {
        private const string Path = ValidationDiagnosticCodes.Path + ".mode";

        /// <summary>The planning mode value is unsupported.</summary>
        public const string Unsupported = Path + ".unsupported";
    }

    /// <summary>Diagnostic codes for solve objectives.</summary>
    public static class Objective {
        private const string Path = ValidationDiagnosticCodes.Path + ".objective";

        /// <summary>The objective value or mode and objective combination is unsupported.</summary>
        public const string Unsupported = Path + ".unsupported";
    }

    /// <summary>Diagnostic codes for numeric values.</summary>
    public static class Number {
        private const string Path = ValidationDiagnosticCodes.Path + ".number";

        /// <summary>A numeric value is NaN or infinite.</summary>
        public const string NonFinite = Path + ".non-finite";

        /// <summary>A numeric value is negative where zero is permitted.</summary>
        public const string Negative = Path + ".negative";

        /// <summary>A numeric value is zero or negative where a positive value is required.</summary>
        public const string PositiveRequired = Path + ".positive-required";
    }
}
