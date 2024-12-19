using System.Text.RegularExpressions;
using System.CommandLine;

var rootCommand = new RootCommand("Quickly rename your files in bulk");

// Define options and arguments
var patternOption = new Option<string>(
    aliases: ["--pattern", "-p"],
    "The pattern to match in the file names")
{
    IsRequired = true
};


var replacementOption = new Option<string>(
    aliases: ["--replacement", "-r"],
    "The replacement for the matched pattern")
{
    IsRequired = true
};

var dryRunOption = new Option<bool>(
    aliases: ["--dry-run", "-d"],
    "Print the changes without actually renaming the files"
);

var filesArgument = new Argument<string[]>(
    "files",
    "The files to rename")
{
    Arity = ArgumentArity.OneOrMore
};
filesArgument.AddValidator(result =>
{
    var files = result.GetValueOrDefault<string[]>();
    if (!files.All(File.Exists))
    {
        result.ErrorMessage = "One or more files do not exist";
    }
});

rootCommand.AddOption(patternOption);
rootCommand.AddOption(replacementOption);
rootCommand.AddOption(dryRunOption);
rootCommand.AddArgument(filesArgument);


// Implement the command handler
rootCommand.SetHandler((pattern, replacement, dryRun, files) =>
{
    var renameItems = files.Select(file =>
    {
        var directory = Path.GetDirectoryName(file);
        var baseName = Path.GetFileName(file);
        var newName = Regex.Replace(baseName, pattern, replacement);
        return new RenameItem(directory ?? ".", baseName, newName);
    });

    // Check if the new names are unique
    var newNames = renameItems.Select(item => item.NewPath);
    if (newNames.Distinct().Count() != newNames.Count())
    {
        Console.WriteLine("Error: New file names are not unique");
        Environment.Exit(1);
    }

    foreach (var item in renameItems)
    {
        if (dryRun)
        {
            Console.WriteLine($"{item.OldPath} --> {item.NewPath}");
        }
        else
        {
            File.Move(item.OldPath, item.NewPath);
        }
    }
},
    patternOption,
    replacementOption,
    dryRunOption,
    filesArgument
);

return await rootCommand.InvokeAsync(args);

record RenameItem(string Directory, string BaseName, string NewName)
{
    public string OldPath => Path.Combine(Directory, BaseName);
    public string NewPath => Path.Combine(Directory, NewName);
}

