

echo "Building Nuget packages"

del build\*.nupkg
mkdir build


.nuget\nuget.exe pack app\Pomona.Common\Pomona.Common.csproj -build -symbols -OutputDirectory build
.nuget\nuget.exe pack app\Pomona\Pomona.csproj -build -symbols -OutputDirectory build
.nuget\nuget.exe pack tests\Pomona.TestHelpers\Pomona.TestHelpers.csproj -build -symbols -OutputDirectory build
.nuget\nuget.exe pack app\TestingClient\TestingClient.csproj -build -symbols -OutputDirectory build
