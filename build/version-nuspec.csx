using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

const string dontRewriteEnv = "PRESERVE_NUSPECVERSION_BRANCHES";
const string buildEnv = "BUILD_BUILDNUMBER";
const string branchEnv = "BUILD_SOURCEBRANCHNAME";
const string shaEnv = "BUILD_SOURCEVERSION";
const string msgEnv = "BUILD_SOURCEVERSIONMESSAGE";

const string nuspec = "Continuous.nuspec";

var build = Environment.GetEnvironmentVariable(buildEnv);
var branch = Environment.GetEnvironmentVariable(branchEnv);
var sha = Environment.GetEnvironmentVariable(shaEnv);
var msg = Environment.GetEnvironmentVariable(msgEnv);
var ignoreBranches = Environment.GetEnvironmentVariable(dontRewriteEnv)?.Split(';')?.ToList() ?? new List<string>();

if (new [] { build, branch, sha, msg }.Any(String.IsNullOrWhiteSpace))
    throw new Exception($"Environment variables are not valid: {new { build, branch, sha, msg }}");

var output = File.ReadAllText(nuspec);

// set release notes to commit
{
    var match = "<releaseNotes>(?<releaseNotes>.*)</releaseNotes>";
    var releaseNotes = $"{sha}: {msg}";
    var replace = $"<releaseNotes>{releaseNotes}</releaseNotes>";
    output = Regex.Replace(output, match, replace);
}

// replace version with build/branch details
if (ignoreBranches.Contains(branch)) 
    Console.WriteLine($"Not altering nuspec version for build from '{branch}' branch");
else
{
    var match = "<version>(?<version>.*)</version>";
    var replacementVersion = $"-b{build}-{branch}";
    var replace = $"<version>${{version}}{replacementVersion}</version>";
    output = Regex.Replace(output, match, replace);

    Console.WriteLine($"Added {replacementVersion} to nuspec version");
}

File.WriteAllText(nuspec, output);
