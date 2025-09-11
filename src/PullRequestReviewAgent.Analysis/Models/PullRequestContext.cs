namespace PullRequestReviewAgent.Analysis.Models;

/// <summary>
/// Represents a pull request context for analysis
/// </summary>
public class PullRequestContext
{
    public string RepositoryOwner { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public int PullRequestNumber { get; set; }
    public string BaseBranch { get; set; } = string.Empty;
    public string HeadBranch { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ChangedFile> ChangedFiles { get; set; } = new List<ChangedFile>();
}

/// <summary>
/// Represents a file that was changed in a pull request
/// </summary>
public class ChangedFile
{
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // added, modified, deleted
    public string? OldContent { get; set; }
    public string? NewContent { get; set; }
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public List<LineChange> Changes { get; set; } = new List<LineChange>();
}

/// <summary>
/// Represents a line change in a file
/// </summary>
public class LineChange
{
    public int LineNumber { get; set; }
    public string ChangeType { get; set; } = string.Empty; // added, removed, modified
    public string Content { get; set; } = string.Empty;
}