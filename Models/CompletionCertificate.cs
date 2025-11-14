namespace PathfinderPhotography.Models;

public class CompletionCertificate
{
    public int Id { get; set; }
    public string PathfinderEmail { get; set; } = string.Empty;
    public string PathfinderName { get; set; } = string.Empty;
    public DateTime CompletionDate { get; set; } = DateTime.UtcNow;
    public byte[] CertificatePdfData { get; set; } = [];
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentDate { get; set; }
}
