using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Genbox.EnumSourceGen.Data;
using Genbox.EnumSourceGen.Generators;
using Genbox.EnumSourceGen.Helpers;
using Genbox.EnumSourceGen.Spec;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Genbox.EnumSourceGen;

[Generator]
public class EnumGenerator : IIncrementalGenerator
{
    private const string DisplayAttribute = "System.ComponentModel.DataAnnotations.DisplayAttribute";
    private const string FlagsAttribute = "System.FlagsAttribute";
    private const string EnumSourceGenAttr = "Genbox.EnumSourceGen." + nameof(EnumSourceGenAttribute);
    private const string EnumTransformAttr = "Genbox.EnumSourceGen." + nameof(EnumTransformAttribute);
    private const string EnumTransformValueAttr = "Genbox.EnumSourceGen." + nameof(EnumTransformValueAttribute);
    private const string EnumOmitValueAttr = "Genbox.EnumSourceGen." + nameof(EnumOmitValueAttribute);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<EnumSpec?> sp = context.SyntaxProvider
                                                         .ForAttributeWithMetadataName(EnumSourceGenAttr, static (node, _) => node is EnumDeclarationSyntax m && m.AttributeLists.Count > 0, Transform)
                                                         .Where(x => x != null);

        var name = GetType().Assembly.GetName();

        string header = $"""
// <auto-generated />
//Generated by {name.Name} {name.Version}
//Generated on: {DateTime.UtcNow:yyyy-MM-dd hh:mm:ss} UTC
#nullable enable

""";

        context.RegisterSourceOutput(sp, (spc, es) =>
        {
            spc.CancellationToken.ThrowIfCancellationRequested();

            //This is only here to mute nullability analysis. We don't have [NotNullWhen].
            if (es == null)
                return;

            try
            {
                StringBuilder sb = new StringBuilder();

                string fqn = es.FullyQualifiedName;

                spc.AddSource(fqn + "_EnumFormat.g.cs", SourceText.From(header + EnumFormatCode.Generate(es), Encoding.UTF8));
                spc.AddSource(fqn + "_Enums.g.cs", SourceText.From(header + EnumClassCode.Generate(es, sb), Encoding.UTF8));
                spc.AddSource(fqn + "_Extensions.g.cs", SourceText.From(header + EnumExtensionCode.Generate(es, sb), Encoding.UTF8));
            }
            catch (Exception e)
            {
                DiagnosticDescriptor report = new DiagnosticDescriptor("ESG001", "EnumSourceGen", $"An error happened while generating code for {es.FullName}. Error: {e.Message}", "errors", DiagnosticSeverity.Error, true);
                spc.ReportDiagnostic(Diagnostic.Create(report, Location.None));
            }
        });
    }

    private static EnumSpec? Transform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol)
            return null;

        bool hasFlags = false;
        EnumSourceGenData? enumSourceGenData = null;
        EnumTransformData? enumTransformData = null;

        foreach (AttributeData ad in symbol.GetAttributes())
        {
            if (ad.AttributeClass == null)
                continue;

            string name = ad.AttributeClass.ToDisplayString();

            if (name.Equals(EnumSourceGenAttr, StringComparison.Ordinal))
                enumSourceGenData = TypeHelper.MapData<EnumSourceGenData>(ad.NamedArguments);
            else if (name.Equals(FlagsAttribute, StringComparison.Ordinal))
                hasFlags = true;
            else if (name.Equals(EnumTransformAttr, StringComparison.Ordinal))
                enumTransformData = TypeHelper.MapData<EnumTransformData>(ad.NamedArguments);
        }

        if (enumSourceGenData == null)
            return null;

        //Now we read attributes applied to members of the enum
        ImmutableArray<ISymbol> enumMembers = symbol.GetMembers();
        List<EnumMemberSpec> members = new List<EnumMemberSpec>(enumMembers.Length);

        bool hasName = false;
        bool hasDescription = false;

        foreach (ISymbol member in enumMembers)
        {
            if (member is not IFieldSymbol field || field.ConstantValue == null)
                continue;

            DisplayData? displayData = null;
            EnumTransformValueData? transformValueData = null;
            EnumOmitValueData? omitValueData = null;

            foreach (AttributeData ad in field.GetAttributes())
            {
                if (ad.AttributeClass == null)
                    continue;

                string name = ad.AttributeClass.ToDisplayString();

                if (name.Equals(DisplayAttribute, StringComparison.Ordinal))
                {
                    displayData = TypeHelper.MapData<DisplayData>(ad.NamedArguments);

                    hasName = displayData.Name != null;
                    hasDescription = displayData.Description != null;
                }
                else if (name.Equals(EnumTransformValueAttr, StringComparison.Ordinal))
                    transformValueData = TypeHelper.MapData<EnumTransformValueData>(ad.NamedArguments);
                else if (name.Equals(EnumOmitValueAttr, StringComparison.Ordinal))
                    omitValueData = TypeHelper.MapData<EnumOmitValueData>(ad.NamedArguments);
            }

            members.Add(new EnumMemberSpec(member.Name, field.ConstantValue, displayData, omitValueData, transformValueData));
        }

        string underlyingType = symbol.EnumUnderlyingType?.Name ?? "int";
        bool isPublic = symbol.DeclaredAccessibility == Accessibility.Public;

        ImmutableArray<SymbolDisplayPart> parts = symbol.ToDisplayParts();

        StringBuilder fqnSb = new StringBuilder(50);
        StringBuilder namespaceSb = new StringBuilder(25);
        StringBuilder enumFullSb = new StringBuilder(25);

        bool inNamespace = false;
        foreach (SymbolDisplayPart part in parts)
        {
            switch (part.Kind)
            {
                case SymbolDisplayPartKind.NamespaceName:
                    inNamespace = true;
                    break;
                case SymbolDisplayPartKind.ClassName:
                case SymbolDisplayPartKind.EnumName:
                    inNamespace = false;
                    break;
                case SymbolDisplayPartKind.Punctuation:
                    break;
            }

            if (inNamespace)
                namespaceSb.Append(part);
            else
                enumFullSb.Append(part);

            fqnSb.Append(part);
        }

        string enumName = symbol.Name;
        string enumFullName = enumFullSb.ToString(); //This include the nested type name (if any)
        string fqn = fqnSb.ToString();
        string? enumNamespace = namespaceSb.Length == 0 ? null : namespaceSb.ToString().TrimEnd('.');

        return new EnumSpec(enumName, enumFullName, fqn, enumNamespace, isPublic, hasName, hasDescription, hasFlags, underlyingType, enumSourceGenData, members, enumTransformData);
    }
}