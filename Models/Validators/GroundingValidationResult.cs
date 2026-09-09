namespace AIChatAssistant.Models.Validators
{
    public class GroundingValidationResult
    {
        public bool Grounded { get; set; }

        //public List<GroundingClaim> Claims { get; set; } = [];

        public string Reason { get; set; } = string.Empty;
    }
}
