using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The rule targets .NET code access security types; Permission is the domain term.",
    Scope = "type",
    Target = "~T:Domain.Permissions.Permission"
)]
