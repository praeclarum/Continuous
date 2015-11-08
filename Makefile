

all:

nuget:
	xbuild /p:Configuration=Release LiveCode.Server.iOS/LiveCode.Server.iOS.csproj
	nuget pack LiveCode.nuspec



