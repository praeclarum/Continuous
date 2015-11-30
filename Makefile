

all:

nuget:
	xbuild /p:Configuration=Release LiveCode.Server.iOS/LiveCode.Server.iOS.csproj
	xbuild /p:Configuration=Release LiveCode.Server.Android/LiveCode.Server.Android.csproj
	nuget pack LiveCode.nuspec

mpack:
	xbuild /p:Configuration=Release LiveCode.Client.MonoDevelop/LiveCode.Client.MonoDevelop.csproj
	cp LiveCode.Client.MonoDevelop/bin/Release/LiveCode.Client.MonoDevelop.dll LiveCode.Client.MonoDevelop/AddinRepo/
	cp LiveCode.Client.MonoDevelop/Properties/addin.info LiveCode.Client.MonoDevelop/AddinRepo/
	cd LiveCode.Client.MonoDevelop/AddinRepo/ && zip LiveCode.Client.MonoDevelop.mpack addin.info LiveCode.Client.MonoDevelop.dll && rm addin.info LiveCode.Client.MonoDevelop.dll
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup rep-build LiveCode.Client.MonoDevelop/AddinRepo

