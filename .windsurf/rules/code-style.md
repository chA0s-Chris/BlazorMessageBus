---
trigger: glob
description: C# code style for BlazorMessageBus, optimized for clarity, consistency, and maintainability.
globs: {src,tests}/**/*.cs
---

## Code Style Guidelines

### General
- Indentation: 4 spaces (no tabs).
- Local variable declarations: prefer `var` by default. Use explicit types only when absolutely necessary to disambiguate intent or when inference is unclear.
- Use CLR types instead of C# aliases (e.g., `Int32`, `Boolean`, `Single`) — enforced via `.editorconfig`.
- Prefer expression-bodied members when they fit cleanly on one line; use block-bodied methods when logic spans multiple lines or readability benefits.
- Use string interpolation over concatenation.
- Use parentheses to clarify precedence in complex expressions when helpful.
- Enable nullable reference types (NRT) and address warnings; annotate reference types using `?` and use the null-forgiving operator `!` sparingly with justification.
- Prefer `record` for immutable/data-transfer types; use `class` for behavior-centric or mutable types.
- Prefer immutability: mark fields as `readonly` where possible and use `init`-only setters for DTOs.

### Naming
- `PascalCase` for public types, methods, properties, events, and enum values.
- `camelCase` for local variables and parameters.
- Private fields: `_camelCase`.
- Private static fields: `_camelCase`.
- Constants (`const`): `PascalCase` (all visibilities).
- Public fields are discouraged; prefer properties.
- Interfaces: `IPascalCase`.

### Namespaces, Usings, and Ordering
- Use file-scoped namespaces.
- Remove unused usings.
- Usings order:
  1. `System.*`
  2. Third-party
  3. Project namespaces
  4. Aliases
  5. `static` usings
- Prefer `nameof(...)` over string literals for symbol names.

### Async and Task-Based APIs
- Async method names must end with `Async`.
- Accept `CancellationToken` as the last parameter for cancellable operations and propagate it.
- Avoid `async void` except for event handlers.
- Do not block on async code (`.Wait()`, `.Result`); always `await`.

### Exceptions and Errors
- Guard arguments early; use `ArgumentNullException(nameof(param))`, `ArgumentException`, or `InvalidOperationException` as appropriate.
- Avoid catching `Exception` broadly; catch specific exception types. When rethrowing, use `throw;` to preserve the stack trace.
- Avoid exception-driven control flow.

### Records vs Classes
- Use `record` for immutable DTOs and value-centric models (supporting with-expressions and value equality).
- Use `class` when encapsulating behavior, when mutation is required, or when reference identity matters.

### Formatting and Language Features
- Favor pattern matching (e.g., `is`, `switch` expressions) when it improves readability.
- Prefer object/collection initializers and target-typed `new(...)` when clear.
- Consider local functions or private methods to extract repeated logic for readability.

### Documentation
- Add XML documentation for all public APIs, including:
  - `<summary>` describing purpose and behavior
  - `<param>` for parameters
  - `<returns>` for return values
  - `<exception>` for expected thrown exceptions
- Prefer self-documenting code and meaningful names; avoid redundant comments.

## Enforcement Notes (.editorconfig)
- CLR types over aliases are intentional and provide better readability in this codebase; enforced via `.editorconfig`.
- `var` is strongly encouraged; prefer `var` in all local declarations unless explicit types notably improve clarity.

Optional .editorconfig snippet (for reference):
```ini
# Prefer framework (CLR) types over language keywords
dotnet_style_predefined_type_for_locals_parameters_members = false:warning
dotnet_style_predefined_type_for_member_access = false:warning

# Prefer var usage broadly
csharp_style_var_for_built_in_types = true:warning
csharp_style_var_when_type_is_apparent = true:warning
csharp_style_var_elsewhere = true:warning

# File-scoped namespaces
csharp_style_namespace_declarations = file_scoped:warning

# Using directives ordering (System first)
dotnet_sort_system_directives_first = true

# Prefer nameof
dotnet_style_nameof_expression = true:suggestion

# Expression-bodied members where short
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = when_on_single_line:suggestion

# Nullable reference types should be enabled in csproj:
# <Nullable>enable</Nullable>