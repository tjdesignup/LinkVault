namespace LinkVault.Application.Files.Events;
public record FileUploadedEvent(
    Guid FileId,          
    string StoredFileName 
);
