using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ssz.Utils.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoSomethingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SSZ0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Dangerous call IDispatcher.BeginInvoke(Func<CancellationToken, Task> asyncAction)",
        messageFormat: "The call to IDispatcher.BeginInvoke(Func<CancellationToken, Task> asyncAction) is forbidden by default. If you fully understand the risk, suppress SSZ0001 locally.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol is null)
            return;

        if (IsForbiddenBeginInvokeOverload(symbol, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }

    private static bool IsForbiddenBeginInvokeOverload(IMethodSymbol method, Compilation compilation)
    {
        var targetInterface = compilation.GetTypeByMetadataName("Ssz.Utils.IDispatcher");
        if (targetInterface is null)
            return false;

        if (!HasTargetSignature(method, compilation))
            return false;

        if (method.ContainingType is null)
            return false;

        // 1. Если сам метод объявлен в интерфейсе IInvoker
        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, targetInterface))
            return true;

        // 2. Если метод принадлежит классу/структуре, реализующей IDispatcher
        foreach (var iface in method.ContainingType.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(iface, targetInterface))
                continue;

            foreach (var interfaceMember in iface.GetMembers("BeginInvoke").OfType<IMethodSymbol>())
            {
                if (!HasTargetSignature(interfaceMember, compilation))
                    continue;

                var implementation = method.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEqualityComparer.Default.Equals(implementation, method))
                    return true;
            }
        }

        return false;
    }

    private static bool HasTargetSignature(IMethodSymbol method, Compilation compilation)
    {
        if (method.Name != "BeginInvoke")
            return false;

        if (method.Parameters.Length != 1)
            return false;

        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        var func2Type = compilation.GetTypeByMetadataName("System.Func`2");

        if (taskType is null || cancellationTokenType is null || func2Type is null)
            return false;

        if (method.Parameters[0].Type is not INamedTypeSymbol parameterType)
            return false;

        if (!SymbolEqualityComparer.Default.Equals(parameterType.OriginalDefinition, func2Type))
            return false;

        if (parameterType.TypeArguments.Length != 2)
            return false;

        return
            SymbolEqualityComparer.Default.Equals(parameterType.TypeArguments[0], cancellationTokenType) &&
            SymbolEqualityComparer.Default.Equals(parameterType.TypeArguments[1], taskType);
    }
}