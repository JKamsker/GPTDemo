# OpenAI Developer Helper

This is a simple (low-effort) tool that uses OpenAI's GPT-4 to help developers understand a new codebase. It's especially useful for onboarding new developers, as it can help them identify relevant classes and files based on a specific question or task.

## How it works

The tool works in two steps:

1. Developers ask a question related to the codebase. The tool then uses OpenAI to identify the classes that could be involved in solving the problem. This process is repeated for a specified number of iterations, simulating multiple developers giving their input.

2. A "senior developer" (also simulated by OpenAI) evaluates the classes identified in the first step. The senior developer provides a final answer, identifying the classes that are most relevant to the problem.

## Usage

The tool requires an OpenAI API key, which should be stored in an environment variable named "OPENAI_API_KEY".

You also need to specify the directory path of the codebase and the number of iterations (developers) to simulate in the constants at the top of the script.

The tool can be run with any .NET runtime that supports C# 9 or later.

## Example

Here's an example of how to use the tool:

```csharp
const string directoryPath = @"C:\Users\W31rd0\source\repos\work\Apro\Apro.AutoUpdater";
const int iterations = 10;
var question = "Where and how do patches and patch files get stored?";

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey =  apiKey,
});

// Run the tool
...
```

In this example, the tool will help a developer understand where and how patches and patch files are stored in the specified codebase.

## Limitations

The tool currently only supports C# codebases and it only identifies classes, not methods or other code structures. It also doesn't provide any context or explanation for why the identified classes are relevant. These limitations could be addressed in future versions ™