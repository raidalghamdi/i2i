using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.TrackAssignments;

public sealed record TrackAssignmentCommandResult(TrackAssignmentCommandStatus Status, EvaluatorTrackAssignment? Assignment = null);
