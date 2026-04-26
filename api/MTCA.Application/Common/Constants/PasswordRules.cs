using System.Text.RegularExpressions;

namespace MTCA.Application.Common.Constants;

public static class PasswordRules
{
    public static readonly Regex Pattern = new(
        $@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9{Regex.Escape(PasswordPolicy.AllowedSpecialCharacters)}]{{{PasswordPolicy.MinimumLength},}}$",
        RegexOptions.Compiled);
}
