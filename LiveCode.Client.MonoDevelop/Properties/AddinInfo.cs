using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"LiveCode.Client.MonoDevelop", 
	Namespace = "LiveCode.Client.MonoDevelop",
	Version = "1.2.4"
)]

[assembly:AddinName ("LiveCode")]
[assembly:AddinCategory ("IDE extensions")]
[assembly:AddinDescription ("Live coding environment visualizes objects as you type them.")]
[assembly:AddinAuthor ("Frank A. Krueger")]

