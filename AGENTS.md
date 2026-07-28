## Nullability

- Treat nullable annotations as authoritative. Do not add null guards, nullable parameters, null-coalescing fallbacks, null-conditional access, or null-forgiving operators for references declared non-nullable.
- Handle null only when the type permits it or an external boundary explicitly requires defensive validation. Fix incorrect annotations at their source instead of contradicting them downstream.
