using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

// Change 20260726 — idea status transitions are spread across eight services. Routing the
// submitter notification through one seam keeps the message wording and audience identical
// wherever a transition happens, instead of duplicating it at each call site.
public interface IIdeaStatusNotifier
{
    Task NotifyStatusChangedAsync(Idea idea, string newStatusCode, CancellationToken cancellationToken = default);
}
