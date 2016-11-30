// ios-repl-swizzle.cs
//
// Copyright 2016 Microsoft.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MonoDevelop.Ide;
using MonoDevelop.Projects;

class SampleAddin
{
    static SampleAddin ()
    {
        IdeApp.ProjectOperations.EndBuild += HandleEndBuild;
        // You might need this event if EndBuild doesn't work
        //IdeApp.ProjectOperations.BeforeStartProject += HandleBeforeStartProject;
    }

    static void HandleEndBuild (object sender, BuildEventArgs e)
    {
        var project = (IdeApp.ProjectOperations.CurrentSelectedSolution?.StartupItem
            ?? IdeApp.ProjectOperations.CurrentSelectedBuildTarget)
            as DotNetProject;
        // Obviously you'll also want to verify that it's a Xamarin.iOS project
        if (project != null)
            Swizzle (project);
    }

    static void Swizzle (DotNetProject project)
    {
		var configSelector = ConfigurationSelector.Default;
        var appdir = Path.ChangeExtension (
			project.GetOutputFileName (configSelector),
            "app");
        var appAssemblyDirs = GetAppBundleAssemblyDirectories (appdir).ToArray ();

        var mscorlib = project
            .AssemblyContext
            .GetAssemblies (project.TargetFramework)
            .FirstOrDefault (asm => asm.Name == "mscorlib");

        var targetFrameworkPath = Path.Combine (
            Path.GetDirectoryName (mscorlib.Location),
            "repl");

        foreach (var asm in Directory.EnumerateFiles (targetFrameworkPath, "*.dll")) {
            foreach (var appAssemblyDir in appAssemblyDirs) {
                File.Copy (asm, Path.Combine (appAssemblyDir, Path.GetFileName (asm)), true);
                var mdb = asm + ".mdb";
                if (File.Exists (mdb))
                    File.Copy (mdb, Path.Combine (appAssemblyDir, Path.GetFileName (mdb)), true);
            }
        }
    }

    static IEnumerable<string> GetAppBundleAssemblyDirectories (string appdir)
    {
        foreach (var appAssemblyDir in new [] {
            appdir, Path.Combine (appdir, ".monotouch-32"), Path.Combine (appdir, ".monotouch-64") })
			if (File.Exists (Path.Combine (appAssemblyDir, "mscorlib.dll")))
                yield return appAssemblyDir;
    }
}
