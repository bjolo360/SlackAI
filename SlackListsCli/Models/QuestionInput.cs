namespace SlackListsCli.Models;

public sealed record QuestionInput(string Question)
{
    public static QuestionInput FromArgs(string[] args)
    {
        var question = string.Empty;
        for (var i = 0; i < args.Length; i += 1)
        {
            if (!args[i].Equals("--question", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 < args.Length)
            {
                question = args[i + 1];
            }
        }

        return new QuestionInput(question);
    }
}
