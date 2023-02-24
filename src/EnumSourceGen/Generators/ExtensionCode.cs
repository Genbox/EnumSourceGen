using System.Text;

namespace Genbox.EnumSourceGen.Generators;

internal static class ExtensionCode
{
    public static string Generate(EnumSpec enumSpec, StringBuilder sb)
    {
        sb.Clear();

        string? extNs = enumSpec.ExtNamespace == null ? null : "\nnamespace " + enumSpec.ExtNamespace + ";\n";
        string extName = enumSpec.ExtName;
        string sn = enumSpec.ExtNamespace == enumSpec.EnumNamespace ? enumSpec.EnumFullName : enumSpec.EnumFullyQualifiedName;
        string visibility = enumSpec.IsPublic ? "public" : "internal";
        string ut = enumSpec.UnderlyingType;

        string res = $$"""
{{(EnumGenerator.Debug ? null : "// <auto-generated />")}}
#nullable enable
using System.Diagnostics.CodeAnalysis;
{{extNs}}
{{visibility}} static partial class {{extName}}
{
    public static string GetString(this {{sn}} value)
        => value switch
        {
            {{GetMemberStrings()}}
            _ => value.ToString()
        };

    public static bool TryGetUnderlyingValue(this {{sn}} value, out {{ut}} underlyingValue)
    {
        switch(value)
        {
{{GetValueStrings()}}
        }
        underlyingValue = default;
        return false;
    }

    public static {{ut}} GetUnderlyingValue(this {{sn}} value)
    {
        if (!TryGetUnderlyingValue(value, out {{ut}} underlyingValue))
            throw new ArgumentOutOfRangeException("Invalid value " + value);

        return underlyingValue;
    }
""";

        if (enumSpec.HasDisplay)
        {
            res +=
                $$"""


    public static bool TryGetDisplayName(this {{sn}} value, [NotNullWhen(true)]out string? displayName)
    {
        switch(value)
        {
{{GetDisplayNames()}}
        }
        displayName = null;
        return false;
    }

    public static string GetDisplayName(this {{sn}} value)
    {
        if (!TryGetDisplayName(value, out string? displayName))
            throw new ArgumentOutOfRangeException("Invalid value " + value);

        return displayName;
    }
""";
        }

        if (enumSpec.HasDescription)
        {
            res +=
                $$"""


    public static bool TryGetDescription(this {{sn}} value, [NotNullWhen(true)]out string? description)
    {
        switch(value)
        {
{{GetDescriptions()}}
        }
        description = null;
        return false;
    }

    public static string GetDescription(this {{sn}} value)
    {
        if (!TryGetDescription(value, out string? description))
            throw new ArgumentOutOfRangeException("Invalid value " + value);

        return description;
    }
""";
        }

        if (enumSpec.HasFlags)
        {
            res +=
                $$"""


    public static bool IsFlagSet(this {{sn}} value, {{sn}} flag)
        => (({{ut}})value & ({{ut}})flag) == ({{ut}})flag;
""";
        }

        string GetMemberStrings()
        {
            sb.Clear();

            for (int i = 0; i < enumSpec.Members.Count; i++)
            {
                EnumMember enumVal = enumSpec.Members[i];

                sb.Append(sn).Append('.').Append(enumVal.Name).Append(" => \"").Append(enumVal.Name).Append("\",\n            ");
            }

            return sb.ToString().TrimEnd();
        }

        string GetValueStrings()
        {
            sb.Clear();

            for (int i = 0; i < enumSpec.Members.Count; i++)
            {
                EnumMember enumVal = enumSpec.Members[i];

                sb.Append(
                    $$"""
            case {{sn}}.{{enumVal.Name}}:
                underlyingValue = {{enumVal.Value}};
                return true;
""");

                if (i != enumSpec.Members.Count - 1)
                    sb.AppendLine();
            }

            return sb.ToString();
        }

        string GetDisplayNames()
        {
            sb.Clear();

            for (int i = 0; i < enumSpec.Members.Count; i++)
            {
                EnumMember enumVal = enumSpec.Members[i];

                if (enumVal.DisplayName == null)
                    continue;

                sb.Append(
                    $$"""
            case {{sn}}.{{enumVal.Name}}:
                displayName = "{{enumVal.DisplayName}}";
                return true;
""");

                if (i != enumSpec.Members.Count - 1)
                    sb.AppendLine();
            }

            return sb.ToString();
        }

        string GetDescriptions()
        {
            sb.Clear();

            for (int i = 0; i < enumSpec.Members.Count; i++)
            {
                EnumMember enumVal = enumSpec.Members[i];

                if (enumVal.Description == null)
                    continue;

                sb.Append(
                    $$"""
            case {{sn}}.{{enumVal.Name}}:
                description = "{{enumVal.Description}}";
                return true;
""");

                if (i != enumSpec.Members.Count - 1)
                    sb.AppendLine();
            }

            return sb.ToString();
        }

        return res + "\n}";
    }
}