using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EvidenceAttachmentConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EvidenceAttachmentConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesActiveAttachmentWithNullDeletedAt()
    {
        Guid attachmentId;
        Guid trackedEntityId;

        using (var context = _fixture.CreateContext())
        {
            var uploaderId = Guid.NewGuid();
            context.Users.Add(new User { Id = uploaderId, SamAccountName = "uploader-t1a", Email = "uploader-t1a@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Uploader" });
            context.SaveChanges();

            trackedEntityId = Guid.NewGuid();
            var attachment = new EvidenceAttachment
            {
                Id = Guid.NewGuid(),
                EntityType = "idea",
                EntityId = trackedEntityId,
                UploaderId = uploaderId,
                FileName = "proposal.pdf",
                BlobPath = "evidence/uploader-t1a/proposal.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 204800,
            };
            attachmentId = attachment.Id;

            context.EvidenceAttachments.Add(attachment);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var attachment = context.EvidenceAttachments.Include(a => a.Uploader).Single(a => a.Id == attachmentId);
            Assert.Equal(trackedEntityId, attachment.EntityId);
            Assert.Equal("uploader-t1a", attachment.Uploader.SamAccountName);
            Assert.Null(attachment.DeletedAt);
        }
    }

    [Fact]
    public void RejectsUploaderIdThatDoesNotExist()
    {
        using var context = _fixture.CreateContext();

        context.EvidenceAttachments.Add(new EvidenceAttachment
        {
            Id = Guid.NewGuid(),
            EntityType = "idea",
            EntityId = Guid.NewGuid(),
            UploaderId = Guid.NewGuid(),
            FileName = "orphan.pdf",
            BlobPath = "evidence/orphan.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
