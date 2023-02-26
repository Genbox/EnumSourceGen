﻿using System.Text;

namespace Genbox.EnumSourceGen.Generators;

internal static class EnumFormatCode
{
    internal static string Generate(EnumSpec enumSpec, StringBuilder sb)
    {
        string res = $$"""
namespace {{enumSpec.EnumsClassNamespace}};

[Flags]
public enum {{enumSpec.EnumName}}Format : byte
{
    None = 0,
    Name = 1,
    Value = 2,
""";

        if (enumSpec.HasDisplay)
        {
            res += """

    DisplayName = 4,
""";
        }

        if (enumSpec.HasDescription)
        {
            res += """

    Description = 8,
""";
        }

        res += """

    Default = Name | Value
}
""";
        return res;
    }
}