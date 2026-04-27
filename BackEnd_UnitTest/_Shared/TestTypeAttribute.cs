using System;

namespace BackEnd_UnitTest._Shared;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TestTypeAttribute : Attribute
{
    public string Type { get; }

    public TestTypeAttribute(string type)
    {
        if (type is not ("N" or "A" or "B"))
            throw new ArgumentException("Type must be 'N' (Normal), 'A' (Abnormal), or 'B' (Boundary).", nameof(type));
        Type = type;
    }
}
