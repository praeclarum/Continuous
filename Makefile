

all:

nuget:
	xbuild /p:Configuration=Release LiveCode.Server.iOS/LiveCode.Server.iOS.csproj
	nuget pack LiveCode.nuspec

mpack:
	xbuild /p:Configuration=Release LiveCode.Client.MonoDevelop/LiveCode.Client.MonoDevelop.csproj
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup pack LiveCode.Client.MonoDevelop/bin/Release/LiveCode.Client.MonoDevelop.dll
	mv LiveCode.Client.MonoDevelop.LiveCode.Client.MonoDevelop_1.0.mpack LiveCode.Client.MonoDevelop/AddinRepo/LiveCode.Client.MonoDevelop_1.0.mpack
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup rep-build LiveCode.Client.MonoDevelop/AddinRepo

