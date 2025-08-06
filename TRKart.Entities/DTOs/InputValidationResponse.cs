namespace TRKart.Entities.DTOs
{
    public class InputValidationResponse
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public string Message { get; set; } = string.Empty;
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
} 