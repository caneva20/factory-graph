## Nullability

- Treat nullable annotations as authoritative. Do not add null guards, nullable parameters, null-coalescing fallbacks, null-conditional access, or null-forgiving operators for references declared non-nullable.
- Handle null only when the type permits it or an external boundary explicitly requires defensive validation. Fix incorrect annotations at their source instead of contradicting them downstream.

## Architecture

- Apply SOLID principles pragmatically. Give each class one cohesive responsibility and one primary reason to change.
- Keep request validation, solve orchestration, model construction, backend execution, and result projection in separate collaborators.
- Keep processors and other entry points thin. Use focused factories as composition roots; constructors should only receive and assign collaborators.
- Isolate native libraries and other infrastructure behind focused adapters. Domain and orchestration code must not depend on backend-specific types.
- Use interfaces for concrete seams such as multiple implementations, external infrastructure, or focused test substitution. Do not create an interface for every class.
- Avoid static service classes. Reserve static classes and members for constants, focused factories, and pure local helpers that need no lifecycle, dependencies, or substitution.
- Put behavior on the type that owns the concept. Use extension methods only for cohesive, reusable operations on the extended type; do not create general utility collections.
- Keep implementation helpers private when they serve one responsibility. Extract a collaborator when behavior gains a separate dependency, policy, lifecycle, or reason to change.
- Review responsibility and dependency direction before completing a change. Refactor working code when responsibilities have become coupled.

## Diagnostics

- Treat diagnostic codes as stable caller-facing contracts.
- Declare each code once in the purpose-specific `*DiagnosticCodes` constants class. Do not emit or assert diagnostic code string literals in C#.
- Keep lifecycle categories separate, such as request validation and solver execution. Add a focused catalog when a new category appears instead of extending an unrelated one.
- Update the catalog before using a new code so one search lists the complete supported set.

## Documentation

- Treat `README.md` as the primary introduction and usage guide. A general .NET developer must be able to understand the library without prior factory-planning or optimization knowledge.
- Introduce each domain term before using it. Explain what the concept represents, why the API needs it, its unit, and how it affects solving or results.
- Explain relationships with one consistent worked example. Keep its names, rates, formulas, code, diagrams, and expected results synchronized.
- Use Mermaid diagrams for topology, flow, and calculations when a diagram communicates the relationship faster than prose.
- Keep the README's mental model complete. Put equations, validation tables, backend details, and other reference material in focused documents, then link them from the relevant README section.
- State current behavior and support boundaries. Do not document planning history, rejected alternatives, or unimplemented behavior as available functionality.
- Define jargon such as steady state, activity, rate, capacity, product outlet, and surplus in plain language. Do not rely on a term's code summary as its explanation.
