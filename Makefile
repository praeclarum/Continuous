

all:

nuget:
	xbuild /p:Configuration=Release LiveCode.Server.iOS/LiveCode.Server.iOS.csproj
	xbuild /p:Configuration=Release LiveCode.Server.Android/LiveCode.Server.Android.csproj
	nuget pack LiveCode.nuspec

mpack:
	xbuild /p:Configuration=Release LiveCode.Client.MonoDevelop/LiveCode.Client.MonoDevelop.csproj
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup pack LiveCode.Client.MonoDevelop/bin/Release/LiveCode.Client.MonoDevelop.dll
	mv LiveCode.Client.MonoDevelop.LiveCode.Client.MonoDevelop_*.mpack LiveCode.Client.MonoDevelop/AddinRepo/LiveCode.Client.MonoDevelop.mpack
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup rep-build LiveCode.Client.MonoDevelop/AddinRepo

