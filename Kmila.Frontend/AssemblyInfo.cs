using System.Runtime.CompilerServices;

// Test suites may register a throw-away language to prove that multi-variant
// tests fail when a variant is missing (HdlLanguages.RegisterForTesting).
[assembly: InternalsVisibleTo("Kmila.Shared.Tests")]
[assembly: InternalsVisibleTo("Kmila.Engine.Tests")]
[assembly: InternalsVisibleTo("Kmila.TestKit")]
