using Circles.Domain.Enums;

namespace Circles.Application.DTOs;

public record PersonDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    bool HasUserAccount,
    string? Email);

public record CircleAccessDto(
    Guid CircleId,
    string Name,
    string Slug,
    CircleType Type,
    Guid? ParentCircleId,
    string AccessKind); // "Direct" or "Derived"

public record OrganizationDto(
    Guid Id,
    string Name,
    string Slug);

/// <summary>Organization overview for the site administrator dashboard.</summary>
public record SiteOrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    int CircleCount,
    int MemberCount,
    DateTime CreatedAt);

public record CircleNodeDto(
    Guid Id,
    string Name,
    string Slug,
    CircleType Type,
    Guid? ParentCircleId,
    List<CircleNodeDto> Children);

public record MemberDto(
    Guid MembershipId,
    Guid PersonId,
    string FullName,
    MembershipRole Role,
    DateTime ValidFrom,
    DateTime? ValidUntil);

public record PermissionsDto(
    Guid PersonId,
    Guid CircleId,
    bool CanAccess,
    List<PermissionType> Permissions);

// ─── Discussion / Post ───────────────────────────────────────────────────────

public record DiscussionSummaryDto(
    Guid Id,
    Guid CircleId,
    string Title,
    string OriginalPosterName,
    int PostCount,
    DateTime CreatedAt,
    DateTime? LatestPostAt,
    Guid? EventId,
    string? EventTitle);

public record DiscussionDetailDto(
    Guid Id,
    Guid CircleId,
    string Title,
    string OriginalPosterName,
    DateTime CreatedAt,
    List<PostDto> Posts,
    Guid? EventId,
    string? EventTitle);

public record PostDto(
    Guid Id,
    Guid PersonId,
    string AuthorName,
    string Content,
    DateTime CreatedAt);

// ─── Poll ────────────────────────────────────────────────────────────────────

public record PollSummaryDto(
    Guid Id,
    Guid CircleId,
    string Title,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    int TotalVotes,
    bool IsOpen);

public record PollDetailDto(
    Guid Id,
    Guid CircleId,
    string Title,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    bool IsOpen,
    List<PollOptionDto> Options,
    Guid? MyVotedOptionId);

public record PollOptionDto(
    Guid Id,
    string Text,
    int Order,
    int VoteCount);

// ─── Task ────────────────────────────────────────────────────────────────────

public record CirclesTaskDto(
    Guid Id,
    Guid CircleId,
    string Title,
    string Description,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? DueDate,
    bool IsCompleted,
    DateTime? CompletedAt,
    Guid? ParentTaskId,
    Guid? AssignedToPersonId,
    string? AssignedToName);


// ─── Admin ───────────────────────────────────────────────────────────────────

/// <summary>A circle as shown in the admin overview (flat, with member count).</summary>
public record AdminCircleDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Slug,
    CircleType Type,
    string? Description,
    Guid? ParentCircleId,
    int MemberCount,
    bool IsArchived);

/// <summary>The enabled/disabled state of one feature module for an organization.</summary>
public record ModuleStatusDto(
    ModuleType Module,
    bool IsEnabled);

/// <summary>Result of inviting a person to a circle by e-mail.</summary>
public record InviteResultDto(
    bool Success,
    string Message,
    MemberDto? Member);
