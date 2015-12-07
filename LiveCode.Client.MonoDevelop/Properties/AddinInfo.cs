using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"Continuous.Client.MonoDevelop", 
	Namespace = "Continuous.Client.MonoDevelop",
	Version = "1.3.4"
)]

[assembly:AddinName ("Continuous")]
[assembly:AddinCategory ("IDE extensions")]
[assembly:AddinDescription ("Live coding environment visualizes objects as you type them.")]
[assembly:AddinAuthor ("Frank A. Krueger")]

