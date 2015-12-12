

all:

nuget:
	xbuild /p:Configuration=Release Continuous.Server.iOS/Continuous.Server.iOS.csproj
	xbuild /p:Configuration=Release Continuous.Server.Android/Continuous.Server.Android.csproj
	nuget pack Continuous.nuspec

mpack:
	xbuild /p:Configuration=Release Continuous.Client.MonoDevelop/Continuous.Client.MonoDevelop.csproj
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup pack Continuous.Client.MonoDevelop/bin/Release/Continuous.Client.MonoDevelop.dll
	mv Continuous.Client.MonoDevelop.Continuous.Client.MonoDevelop_*.mpack Continuous.Client.MonoDevelop/AddinRepo/Continuous.Client.MonoDevelop.mpack
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup rep-build Continuous.Client.MonoDevelop/AddinRepo

