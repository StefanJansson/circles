using Circles.Application.DTOs;
using Circles.Domain.Entities;
using Circles.Domain.Enums;
using Circles.Domain.Interfaces;
using Circles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Circles.Application.Services;

/// <summary>
/// Write + read operations for Discussions, Polls and Tasks.
/// Authorization is always checked first — callers pass in the actingPersonId
/// and the service throws UnauthorizedAccessException if the person lacks
/// the required permission.
/// </summary>
public class ContentService
{
    private readonly CirclesDbContext _db;
    private readonly IAuthorizationService _authz;

    public ContentService(CirclesDbContext db, IAuthorizationService authz)
    {
        _db = db;
        _authz = authz;
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private async Task RequirePermissionAsync(Guid personId, Guid circleId, PermissionType perm)
    {
        var perms = await _authz.GetPersonPermissionsInCircleAsync(personId, circleId);
        if (!perms.Contains(perm))
            throw new UnauthorizedAccessException($"Saknar behörighet: {perm}");
    }

    // ─── Discussions ─────────────────────────────────────────────────────────

    public async Task<List<DiscussionSummaryDto>> GetDiscussionsAsync(Guid circleId, Guid actingPersonId)
    {
        await RequirePermissionAsync(actingPersonId, circleId, PermissionType.ReadPosts);

        // Order BEFORE Select so EF Core can translate to SQL.
        // Sorting key: most recent post, or discussion creation time if no posts.
        return await _db.Discussions
            .Where(d => d.CircleId == circleId)
            .OrderByDescending(d => d.Posts.Any()
                ? d.Posts.Max(p => (DateTime?)p.CreatedAt)
                : (DateTime?)d.CreatedAt)
            .Select(d => new DiscussionSummaryDto(
                d.Id,
                d.CircleId,
                d.Title,
                d.OriginalPoster!.FirstName + " " + d.OriginalPoster.LastName,
                d.Posts.Count,
                d.CreatedAt,
                d.Posts.Any() ? d.Posts.Max(p => (DateTime?)p.CreatedAt) : null,
                d.EventId,
                d.Event != null ? d.Event.Title : null))
            .ToListAsync();
    }

    public async Task<DiscussionDetailDto?> GetDiscussionAsync(Guid discussionId, Guid actingPersonId)
    {
        var d = await _db.Discussions
            .Include(d => d.OriginalPoster)
            .Include(d => d.Event)
            .Include(d => d.Posts).ThenInclude(p => p.Person)
            .FirstOrDefaultAsync(d => d.Id == discussionId);

        if (d is null) return null;

        await RequirePermissionAsync(actingPersonId, d.CircleId, PermissionType.ReadPosts);

        return new DiscussionDetailDto(
            d.Id,
            d.CircleId,
            d.Title,
            d.OriginalPoster!.FullName,
            d.CreatedAt,
            d.Posts
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PostDto(
                    p.Id,
                    p.PersonId,
                    p.Person!.FullName,
                    p.Content,
                    p.CreatedAt))
                .ToList(),
            d.EventId,
            d.Event?.Title);
    }

    public async Task<Guid> CreateDiscussionAsync(
        Guid circleId, Guid personId, string title, string firstPost, Guid? eventId = null)
    {
        await RequirePermissionAsync(personId, circleId, PermissionType.CreateDiscussion);

        // Only accept an event link that belongs to the same circle.
        Guid? validEventId = null;
        if (eventId is { } eid)
        {
            var belongs = await _db.Events.AnyAsync(e => e.Id == eid && e.CircleId == circleId);
            if (belongs) validEventId = eid;
        }

        var discussion = new Discussion
        {
            Id = Guid.NewGuid(),
            CircleId = circleId,
            OriginalPosterPersonId = personId,
            Title = title.Trim(),
            EventId = validEventId,
            CreatedAt = DateTime.UtcNow
        };

        var post = new Post
        {
            Id = Guid.NewGuid(),
            DiscussionId = discussion.Id,
            PersonId = personId,
            Content = firstPost.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Discussions.Add(discussion);
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        return discussion.Id;
    }

    public async Task<Guid> AddPostAsync(Guid discussionId, Guid personId, string content)
    {
        var d = await _db.Discussions.FindAsync(discussionId)
            ?? throw new KeyNotFoundException("Diskussion hittades inte.");

        await RequirePermissionAsync(personId, d.CircleId, PermissionType.Comment);

        var post = new Post
        {
            Id = Guid.NewGuid(),
            DiscussionId = discussionId,
            PersonId = personId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        return post.Id;
    }

    // ─── Polls ────────────────────────────────────────────────────────────────

    public async Task<List<PollSummaryDto>> GetPollsAsync(Guid circleId, Guid actingPersonId)
    {
        await RequirePermissionAsync(actingPersonId, circleId, PermissionType.ReadPosts);

        return await _db.Polls
            .Where(p => p.CircleId == circleId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PollSummaryDto(
                p.Id,
                p.CircleId,
                p.Title,
                p.CreatedAt,
                p.ClosedAt,
                p.Options.SelectMany(o => o.Votes).Count(),
                p.ClosedAt == null || p.ClosedAt > DateTime.UtcNow))
            .ToListAsync();
    }

    public async Task<PollDetailDto?> GetPollAsync(Guid pollId, Guid actingPersonId)
    {
        var poll = await _db.Polls
            .Include(p => p.Options).ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll is null) return null;

        await RequirePermissionAsync(actingPersonId, poll.CircleId, PermissionType.ReadPosts);

        var myVote = poll.Options
            .SelectMany(o => o.Votes)
            .FirstOrDefault(v => v.PersonId == actingPersonId);

        return new PollDetailDto(
            poll.Id,
            poll.CircleId,
            poll.Title,
            poll.CreatedAt,
            poll.ClosedAt,
            poll.ClosedAt == null || poll.ClosedAt > DateTime.UtcNow,
            poll.Options
                .OrderBy(o => o.Order)
                .Select(o => new PollOptionDto(o.Id, o.Text, o.Order, o.Votes.Count))
                .ToList(),
            myVote?.PollOptionId);
    }

    public async Task<Guid> CreatePollAsync(
        Guid circleId, Guid personId, string title, List<string> options)
    {
        await RequirePermissionAsync(personId, circleId, PermissionType.CreatePoll);

        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            CircleId = circleId,
            Title = title.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        for (var i = 0; i < options.Count; i++)
            poll.Options.Add(new PollOption
            {
                Id = Guid.NewGuid(),
                PollId = poll.Id,
                Text = options[i].Trim(),
                Order = i
            });

        _db.Polls.Add(poll);
        await _db.SaveChangesAsync();
        return poll.Id;
    }

    public async Task VoteAsync(Guid pollId, Guid optionId, Guid personId)
    {
        var poll = await _db.Polls
            .Include(p => p.Options).ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(p => p.Id == pollId)
            ?? throw new KeyNotFoundException("Omröstning hittades inte.");

        await RequirePermissionAsync(personId, poll.CircleId, PermissionType.Vote);

        if (poll.ClosedAt.HasValue && poll.ClosedAt < DateTime.UtcNow)
            throw new InvalidOperationException("Omröstningen är stängd.");

        if (!poll.Options.Any(o => o.Id == optionId))
            throw new ArgumentException("Alternativet tillhör inte den här omröstningen.");

        // Remove existing vote from this person (change vote)
        var existing = poll.Options
            .SelectMany(o => o.Votes)
            .FirstOrDefault(v => v.PersonId == personId);
        if (existing is not null)
            _db.Votes.Remove(existing);

        _db.Votes.Add(new Vote
        {
            Id = Guid.NewGuid(),
            PollOptionId = optionId,
            PersonId = personId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    // ─── Tasks ────────────────────────────────────────────────────────────────

    public async Task<List<CirclesTaskDto>> GetTasksAsync(Guid circleId, Guid actingPersonId)
    {
        await RequirePermissionAsync(actingPersonId, circleId, PermissionType.ReadPosts);

        return await _db.Tasks
            .Where(t => t.CircleId == circleId)
            .OrderBy(t => t.CompletedAt.HasValue)        // ej klara först
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue) // närmast deadline
            .ThenBy(t => t.CreatedAt)
            .Select(t => new CirclesTaskDto(
                t.Id,
                t.CircleId,
                t.Title,
                t.Description,
                t.CreatedBy!.FirstName + " " + t.CreatedBy.LastName,
                t.CreatedAt,
                t.DueDate,
                t.CompletedAt.HasValue,
                t.CompletedAt,
                t.ParentTaskId,
                t.AssignedToPersonId,
                t.AssignedTo != null
                    ? t.AssignedTo.FirstName + " " + t.AssignedTo.LastName
                    : null))
            .ToListAsync();
    }

    public async Task<Guid> CreateTaskAsync(
        Guid circleId, Guid personId, string title, string description, DateTime? dueDate,
        Guid? parentTaskId = null, Guid? assignedToPersonId = null)
    {
        await RequirePermissionAsync(personId, circleId, PermissionType.CreateTask);

        // A sub-task's parent must be an existing task in the same circle.
        // Only one level of nesting is allowed (a sub-task cannot have children).
        Guid? validParentId = null;
        if (parentTaskId is { } pid)
        {
            var parent = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == pid && t.CircleId == circleId)
                ?? throw new InvalidOperationException("Den valda överuppgiften finns inte i cirkeln.");
            if (parent.ParentTaskId is not null)
                throw new InvalidOperationException("En deluppgift kan inte ha egna deluppgifter.");
            validParentId = pid;
        }

        // The assignee must be an active member of the circle.
        Guid? validAssigneeId = null;
        if (assignedToPersonId is { } aid)
        {
            var isMember = await _db.Memberships.AnyAsync(m =>
                m.CircleId == circleId
                && m.PersonId == aid
                && m.ValidFrom <= DateTime.UtcNow
                && (m.ValidUntil == null || m.ValidUntil > DateTime.UtcNow));
            if (isMember) validAssigneeId = aid;
        }

        var task = new CirclesTask
        {
            Id = Guid.NewGuid(),
            CircleId = circleId,
            CreatedByPersonId = personId,
            Title = title.Trim(),
            Description = description.Trim(),
            DueDate = dueDate,
            ParentTaskId = validParentId,
            AssignedToPersonId = validAssigneeId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return task.Id;
    }

    public async Task CompleteTaskAsync(Guid taskId, Guid personId)
    {
        var task = await _db.Tasks.FindAsync(taskId)
            ?? throw new KeyNotFoundException("Uppgift hittades inte.");

        await RequirePermissionAsync(personId, task.CircleId, PermissionType.CreateTask);

        task.CompletedAt = task.CompletedAt.HasValue ? null : DateTime.UtcNow; // toggle
        await _db.SaveChangesAsync();
    }
}
