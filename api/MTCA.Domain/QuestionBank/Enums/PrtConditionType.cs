namespace MTCA.Domain.QuestionBank.Enums;

public enum PrtConditionType
{
    EXACT_MATCH = 0,
    EQUIVALENT = 1,
    DIFFERS_BY_CONSTANT = 2,
    MISSING_FACTOR = 3,
    WRONG_SIGN = 4,
    CONTAINS_SUBEXPR = 5,
    FALLBACK = 6
}
