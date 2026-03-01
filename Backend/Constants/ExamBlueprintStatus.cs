namespace Backend.Constants
{
    public static class ExamBlueprintStatus
    {
        public const int Draft = 0;
        public const int Published = 1;
        public const int InUse = 2;
        public const int Archived = 3;

        public static string GetLabel(int status)
        {
            return status switch
            {
                Draft => "Draft",
                Published => "Published",
                InUse => "In Use",
                Archived => "Archived",
                _ => "Unknown"
            };
        }
    }
}
