namespace ApparkaTrainingFlowOnline.Models;

public class TrainingOptions
{
    public int ActivityPassingScore { get; set; }
    public int FinalExamPassingScore { get; set; } = 80;
    public int FinalExamMaxAttempts { get; set; } = 3;
    public int ValidationCodeMinutes { get; set; } = 10;
}

public class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Apparka Training Flow";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
