using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Analyzer.Tests;

public class UnitTest1
{
    private class Verify<TCodeFix> : CodeFixVerifier<NamespaceAnalyzer, TCodeFix, CSharpCodeFixTest<NamespaceAnalyzer, TCodeFix>, DefaultVerifier>
            where TCodeFix : CodeFixProvider, new()
    {
    }



    [Fact]
    public async Task Test1()
    {
        var before =
                """
                namespace SpecificNamespace;

                public class Class1
                {
                    public void Run()
                    {
                        // should call Process of SpecificNamespace.ISpecificInterface
                    }

                }


                public interface ISpecificInterface
                {
                    void Process();
                }
                """;

        var after =
            """
            namespace SpecificNamespace;

            public class Class1
            {
                public void Run(SpecificNamespace.ISpecificInterface specificInterface)
                {
                    specificInterface.Process();
                    // should call Process of SpecificNamespace.ISpecificInterface
                }

            }


            public interface ISpecificInterface
            {
                void Process();
            }
            """;

        var diagnostic = DiagnosticResult.CompilerWarning("NamespaceAnalyzer").WithSpan(5, 17, 5, 20).WithArguments("Run", "SpecificNamespace");
        await Verify<NamespaceCodeFixProvider>.VerifyCodeFixAsync(before, diagnostic, after);
    }
}

internal class CSharpCodeFixTest<TAnalyzer, TCodeFix> : CodeFixTest<DefaultVerifier>
      where TAnalyzer : DiagnosticAnalyzer, new()
      where TCodeFix : CodeFixProvider, new()
{
    public sealed override string Language => LanguageNames.CSharp;

    public sealed override Type SyntaxKindType => typeof(SyntaxKind);

    protected sealed override string DefaultFileExt => "cs";

    protected override CompilationOptions CreateCompilationOptions()
        => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

    protected override ParseOptions CreateParseOptions()
        => new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
    {
        yield return new TAnalyzer();
    }

    protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
    {
        yield return new TCodeFix();
    }
}