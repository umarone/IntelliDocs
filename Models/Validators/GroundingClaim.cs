namespace AIChatAssistant.Models.Validators
{
    public class GroundingClaim
    {
        public string Claim { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;

        public bool Supported { get; set; }
    }
}
