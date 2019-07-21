

all:

nuget:
	nuget restore Continuous.sln
	msbuild /p:Configuration=Release Continuous.Server.iOS/Continuous.Server.iOS.csproj
	msbuild /p:Configuration=Release Continuous.Server.Android/Continuous.Server.Android.csproj
	nuget pack Continuous.nuspec

mpack:
	nuget restore Continuous.sln
	msbuild /p:Configuration=Release Continuous.Client.MonoDevelop/Continuous.Client.MonoDevelop.csproj
	/Applications/Visual\ Studio.app/Contents/MacOS/vstool setup pack Continuous.Client.MonoDevelop/bin/Release/Continuous.Client.MonoDevelop.dll
	mv Continuous.Client.MonoDevelop.Continuous.Client.MonoDevelop_*.mpack Continuous.Client.MonoDevelop/AddinRepo/Continuous.Client.MonoDevelop.mpack
	/Applications/Visual\ Studio.app/Contents/MacOS/vstool setup rep-build Continuous.Client.MonoDevelop/AddinRepo

