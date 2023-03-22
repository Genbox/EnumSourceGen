﻿using System.Globalization;
using System.Text;
using Genbox.EnumSourceGen.Misc;
using static Genbox.EnumSourceGen.Helpers.CodeGenHelper;

namespace Genbox.EnumSourceGen.Generators;

internal static class EnumClassCode
{
    internal static string Generate(EnumSpec es, AttributeOptions op, StringBuilder sb)
    {
        sb.Clear();

        string? ns = op.EnumsClassNamespace ?? es.Namespace;
        string cn = op.EnumNameOverride ?? es.Name;
        string en = op.EnumsClassName ?? "Enums";
        string sn = es.FullyQualifiedName; //We always use FQN. The class name is the same as the enum name
        string vi = es.IsPublic ? "public" : "internal";
        string ut = es.UnderlyingType;
        string mc = es.Members.Count.ToString(NumberFormatInfo.InvariantInfo);
        string ef = (ns != null ? ns + '.' : null) + cn + "Format";

        string res = $$"""
// <auto-generated />
#nullable enable
{{(ns != null ? "\nnamespace " + ns + ";\n" : null)}}
public static partial class {{en}}
{
    {{vi}} static partial class {{cn}}
    {
        public const int MemberCount = {{mc}};
        public const bool IsFlagEnum = {{es.HasFlags.ToString().ToLowerInvariant()}};

        private static string[]? _names;
        public static string[] GetMemberNames()
            => _names ??= new[] {
                {{GetMemberNames()}}
            };

        private static {{sn}}[]? _values;
        public static {{sn}}[] GetMemberValues()
            => _values ??= new[] {
                {{GetMemberValues()}}
            };

        private static {{ut}}[]? _underlyingValues;
        public static {{ut}}[] GetUnderlyingValues()
            => _underlyingValues ??= new {{ut}}[] {
                {{GetUnderlyingValues()}}
            };

        public static bool TryParse(string value, out {{sn}} result, {{ef}} format = {{ef}}.Default, StringComparison comparison = StringComparison.Ordinal)
        {
{{GetTryParseSwitch()}}

            result = default;
            return false;
        }

        public static bool TryParse(ReadOnlySpan<char> value, out {{sn}} result, {{ef}} format = {{ef}}.Default, StringComparison comparison = StringComparison.Ordinal)
        {
{{GetTryParseSwitch()}}

            result = default;
            return false;
        }

        public static {{sn}} Parse(ReadOnlySpan<char> value, {{ef}} format = {{ef}}.Default, StringComparison comparison = StringComparison.Ordinal)
        {
            if (!TryParse(value, out {{sn}} result, format, comparison))
                throw new ArgumentOutOfRangeException($"Invalid value: {value}");

            return result;
        }

        public static {{sn}} Parse(string value, {{ef}} format = {{ef}}.Default, StringComparison comparison = StringComparison.Ordinal)
        {
            if (!TryParse(value, out {{sn}} result, format, comparison))
                throw new ArgumentOutOfRangeException($"Invalid value: {value}");

            return result;
        }

        public static bool IsDefined({{sn}} input) => {{GetIsDefined(es, ut)}};
""";

        if (es.HasDisplay)
        {
            res +=
                $$"""


        private static ({{sn}}, string)[]? _displayNames;
        public static ({{sn}}, string)[] GetDisplayNames()
            => _displayNames ??= new [] {
                {{GetDisplayNames()}}
            };
""";
        }

        if (es.HasDescription)
        {
            res +=
                $$"""


        private static ({{sn}}, string)[]? _descriptions;
        public static ({{sn}}, string)[] GetDescriptions()
            => _descriptions ??= new[] {
                {{GetDescriptions()}}
            };
""";
        }

        string GetMemberNames()
        {
            sb.Clear();

            for (int i = 0; i < es.Members.Count; i++)
            {
                EnumMember enumVal = es.Members[i];
                sb.Append('"').Append(enumVal.Name).Append("\",\n").Append(Indent(4));
            }

            return sb.ToString().TrimEnd(CodeConstants.TrimChars);
        }

        string GetMemberValues()
        {
            sb.Clear();

            for (int i = 0; i < es.Members.Count; i++)
                sb.Append(sn).Append('.').Append(es.Members[i].Name).Append(",\n").Append(Indent(4));

            return sb.ToString().TrimEnd(CodeConstants.TrimChars);
        }

        string GetUnderlyingValues()
        {
            sb.Clear();

            for (int i = 0; i < es.Members.Count; i++)
                sb.Append(es.Members[i].Value).Append(",\n").Append(Indent(4));

            return sb.ToString().TrimEnd(CodeConstants.TrimChars);
        }

        string GetTryParseSwitch()
        {
            sb.Clear();

            sb.Append($$"""

            if (format.HasFlag({{ef}}.Name))
            {
""");

            for (int i = 0; i < es.Members.Count; i++)
            {
                EnumMember enumVal = es.Members[i];

                sb.Append($$"""

                if (value.Equals("{{enumVal.Name}}", comparison))
                {
                    result = {{sn}}.{{enumVal.Name}};
                    return true;
                }
""");

                if (i != es.Members.Count - 1)
                    sb.AppendLine();
            }

            sb.Append("\n            }");

            sb.Append($$"""

            if (format.HasFlag({{ef}}.Value))
            {
""");

            for (int i = 0; i < es.Members.Count; i++)
            {
                EnumMember enumVal = es.Members[i];

                sb.Append($$"""
                if (value.Equals("{{enumVal.Value}}", comparison))
                {
                    result = {{sn}}.{{enumVal.Name}};
                    return true;
                }
""");

                if (i != es.Members.Count - 1)
                    sb.AppendLine();
            }

            sb.Append("\n            }");

            if (es.HasDisplay)
            {
                sb.Append($$"""

            if (format.HasFlag({{ef}}.DisplayName))
            {
""");

                for (int i = 0; i < es.Members.Count; i++)
                {
                    EnumMember enumVal = es.Members[i];

                    if (enumVal.DisplayName != null)
                    {
                        sb.Append($$"""

                if (value.Equals("{{enumVal.DisplayName}}", comparison))
                {
                    result = {{sn}}.{{enumVal.Name}};
                    return true;
                }
""");
                    }
                    if (i != es.Members.Count - 1)
                        sb.AppendLine();
                }

                sb.Append("\n            }");
            }

            if (es.HasDescription)
            {
                sb.Append($$"""

            if (format.HasFlag({{ef}}.Description))
            {
""");

                for (int i = 0; i < es.Members.Count; i++)
                {
                    EnumMember enumVal = es.Members[i];

                    if (enumVal.Description != null)
                    {
                        sb.Append($$"""

                if (value.Equals("{{enumVal.Description}}", comparison))
                {
                    result = {{sn}}.{{enumVal.Name}};
                    return true;
                }
""");
                    }

                    if (i != es.Members.Count - 1)
                        sb.AppendLine();
                }

                sb.Append("\n            }");
            }

            return sb.ToString();
        }

        string GetDisplayNames()
        {
            sb.Clear();

            for (int i = 0; i < es.Members.Count; i++)
            {
                EnumMember enumVal = es.Members[i];

                if (enumVal.DisplayName == null)
                    continue;

                sb.Append('(').Append(sn).Append('.').Append(enumVal.Name).Append(", \"").Append(enumVal.DisplayName).Append("\"),\n").Append(Indent(4));
            }

            return sb.ToString().TrimEnd(CodeConstants.TrimChars);
        }

        string GetDescriptions()
        {
            sb.Clear();

            for (int i = 0; i < es.Members.Count; i++)
            {
                EnumMember enumVal = es.Members[i];

                if (enumVal.Description == null)
                    continue;

                sb.Append('(').Append(sn).Append('.').Append(enumVal.Name).Append(", \"").Append(enumVal.Description).Append("\"),\n").Append(Indent(4));
            }

            return sb.ToString().TrimEnd(CodeConstants.TrimChars);
        }

        string GetIsDefined(EnumSpec spec, string underlyingType)
        {
            if (spec.Members.Count == 0)
                return "false";

            ulong value = 0;

            foreach (EnumMember member in spec.Members)
                value |= (ulong)Convert.ChangeType(member.Value, typeof(ulong));

            if (value == 0)
                return $"0 == ({underlyingType})input";

            return "(0b" + Convert.ToString(unchecked((long)value), 2) + $" & ({underlyingType})input) == ({underlyingType})input";
        }

        return res + "\n    }\n}";
    }
}