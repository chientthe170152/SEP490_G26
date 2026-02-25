namespace Backend.Exceptions
{
    public sealed class ExamBlueprintValidationException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public ExamBlueprintValidationException(IEnumerable<string> errors)
            : base("Exam blueprint validation failed.")
        {
            Errors = errors.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();
        }
    }
}
