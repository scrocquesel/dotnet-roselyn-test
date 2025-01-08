using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NamespaceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NamespaceAnalyzer";
    private static readonly LocalizableString Title = "Run method must call Process method";
    private static readonly LocalizableString MessageFormat = "Method '{0}' in namespace '{1}' must call a Process method on a type implementing ISpecificInterface";
    private static readonly LocalizableString Description = "Ensure that the Run method calls a Process method on a type implementing ISpecificInterface.";
    private const string Category = "Usage";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        if (methodDeclaration.Identifier.Text != "Run")
        {
            return;
        }

        var containingNamespace = methodDeclaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        if (containingNamespace == null || !containingNamespace.Name.ToString().StartsWith("SpecificNamespace"))
        {
            return;
        }


        
        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        var interfaceType = semanticModel.Compilation.GetTypeByMetadataName("SpecificNamespace.ISpecificInterface");

        if (interfaceType == null)
        {
            return;
        }

        var callsProcessMethod = methodDeclaration.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol)
            .Any(method => method != null && method.Name == "Process" && method.ContainingType.AllInterfaces.Contains(interfaceType));

        if (!callsProcessMethod)
        {
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text, containingNamespace.Name.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
