

all:

nuget:
	xbuild /p:Configuration=Release Continuous.Server.iOS/Continuous.Server.iOS.csproj
	xbuild /p:Configuration=Release Continuous.Server.Android/Continuous.Server.Android.csproj
	nuget pack Continuous.nuspec

mpack:
	xbuild /p:Configuration=Release Continuous.Client.MonoDevelop/Continuous.Client.MonoDevelop.csproj
	cp Continuous.Client.MonoDevelop/bin/Release/Continuous.Client.MonoDevelop.dll Continuous.Client.MonoDevelop/AddinRepo/
	cp Continuous.Client.MonoDevelop/Properties/addin.info Continuous.Client.MonoDevelop/AddinRepo/
	cd Continuous.Client.MonoDevelop/AddinRepo/ && zip Continuous.Client.MonoDevelop.mpack addin.info Continuous.Client.MonoDevelop.dll && rm addin.info Continuous.Client.MonoDevelop.dll
	/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool setup rep-build Continuous.Client.MonoDevelop/AddinRepo

