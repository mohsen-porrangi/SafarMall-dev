namespace BuildingBlocks.Contracts.Options
{
    public class AuthenticationOptions
    {
        public const string Name = "Authentication";
        public required string SecretKey { get; set; }
        public required string Audience { get; set; }
        public required string Issuer { get; set; }
        public required double TokenExpirationMinutes { get; set; }
    }
}
