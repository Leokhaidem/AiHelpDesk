namespace AIHelpDesk.Domain.Entities;

/// <summary>
/// Metadata record for a file attachment uploaded to a ticket.
/// The actual file bytes are stored separately (local disk in dev, Azure Blob in prod).
/// </summary>
public class Attachment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    /// <summary>Original filename as supplied by the user.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Safe storage key / path (GUID-based, never user-supplied).</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Identity user ID of whoever uploaded the file.</summary>
    public string UploaderId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Ticket? Ticket { get; set; }
}
