using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey =  apiKey,
});

const int iterations = 10;
// var question = "How do I make the logging messages arrive faster in the file?";
var question = "Where and how do patches and patch files get stored?";

Console.WriteLine("Developers give answers to questions");
var allRelevantClasses = new ConcurrentBag<string>();
await Parallel.ForEachAsync(Enumerable.Range(0, iterations), async (i, cts) =>
{
    Console.WriteLine($"Starting iteration {i} of {iterations}");
    var relevantClasses = await GetRelevantClasses(openAiService, question);
    if (string.IsNullOrEmpty(relevantClasses))
    {
        Console.WriteLine("No relevant classes found");
        return;
    }

    allRelevantClasses.Add(relevantClasses);
    Console.WriteLine($"Developer {i} identified the following classes as relevant to the problem:" + Environment.NewLine + relevantClasses);
    Console.WriteLine($"Finished iteration {i} of {iterations}");
});

var sb = new StringBuilder();
foreach (var relevantClasses in allRelevantClasses.Select((x, i) => (x, i)))
{
    sb.AppendLine("---");
    sb.AppendLine($"Developer {relevantClasses.i + 1} identified the following classes as relevant to the problem:");
    sb.AppendLine(relevantClasses.x);
}

Console.WriteLine("Senior developer evaluates answers");
var finalAnswer = await EvaluateRelevantClasses(openAiService, question, sb.ToString());

Debugger.Break();


static async Task<string> GetRelevantClasses(OpenAIService openAiService, string question)
{
    var classes = GetClasses()
    .Select(x => $"File: {x.file}, Class: {x.className}")
    .Aggregate((x, y) => $"{x}\n{y}");

    var message = $"""
We have recently onboarded a new developer to our team. 
You are tasked with helping them get started with a new codebase.
The new developer has a question: "{question}"
First, we need to identify the classes that could be involved in solving the develper's problem.
You are given a list of classes in the codebase and asked to identify the classes that could be involved in solving the problem.
First, think about the problem and which classes could be involved in solving it.
Then, write down a short explanation of your thought process before giving your answer. Repeat this process until you have identified a sufficient number of classes.
Here is a list of classes in the codebase:
{classes}
""".Trim('\n', '\r');

    var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
    {
        Messages = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are a developer, trying to help a fellow with getting started with a new codebase."),
            ChatMessage.FromUser(message),
        },
        Model = Models.Gpt_3_5_Turbo_16k,
    });

    if (completionResult.Successful)
    {
        return completionResult.Choices.FirstOrDefault()?.Message.Content;
    }
    return string.Empty;
}

static async Task<string> EvaluateRelevantClasses(OpenAIService openAiService, string question, string relevantClasses)
{
    var message = $"""
We have recently onboarded a new developer to our team. 
You are tasked with helping them get started with a new codebase.
The new developer has a question: "{question}"
Your colleague has identified the following classes as relevant to the problem:
{relevantClasses}
---
Evaluate the classes that your colleague has identified and based on their evaluation, answer with the classes that you think are relevant to the problem.
""".Trim('\n', '\r');


    var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
    {
        Messages = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are a senior developer, trying to help a fellow with getting started with a new codebase."),
            ChatMessage.FromUser(message),
        },
        Model = Models.Gpt_4,
    });

    Console.WriteLine("Evaluating relevant classes");
    var sb = new StringBuilder();
    await foreach (var completion in completionResult)
    {
        if (!completion.Successful)
        {
            Console.WriteLine($"Error: {completion.Error}");
            continue;
        }

        Console.Write(completion.Choices.FirstOrDefault()?.Message.Content);
        sb.Append(completion.Choices.FirstOrDefault()?.Message.Content);
    }
    return sb.ToString();
}


static IEnumerable<(string file, string className)> GetClasses()
{

    string directoryPath = @"C:\Users\W31rd0\source\repos\work\Apro\Apro.AutoUpdater";

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory '{directoryPath}' does not exist.");
        yield break;
    }

    // var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories).Where(x => !x.Contains(".submodules")).ToList();

    var files = Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories).Where(x => !x.Contains(".submodules"));
    Regex regex = new Regex(@"class\s+(.*?)\s", RegexOptions.Singleline);

    foreach (string file in files)
    {
        string content = File.ReadAllText(file);
        MatchCollection matches = regex.Matches(content);

        foreach (Match match in matches)
        {
            // Console.WriteLine($"File: {file.Replace(directoryPath, "")}, Class: {match.Groups[1].Value}");
            yield return (file.Replace(directoryPath, ""), match.Groups[1].Value);
        }
    }
}