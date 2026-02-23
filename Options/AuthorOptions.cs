using System.Collections.Generic;

namespace DialogueHelper.Options;

public class AuthorOptions
{
    public string Name = "";
    public List<string> Groups = [];
    
    public AuthorOptions Clone()
    {
        return new()
        {
            Name = Name,
            Groups = [..Groups],
        };
    }
}
