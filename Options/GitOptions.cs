namespace DialogueHelper.Options;

public class GitOptions
{
    public bool IsEnabled;
    public string RepoUrl = "";
    public string Branch = "";

    public GitOptions Clone()
    {
        return new()
        {
            IsEnabled = IsEnabled,
            Branch = Branch,
            RepoUrl = RepoUrl,
        };
    }
}
