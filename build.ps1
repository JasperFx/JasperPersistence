$ErrorActionPreference = "Stop";
$version = dotnet --version;
if ($version.StartsWith("6.")) {
    $target_framework = "net6.0"
} 
elseif ($version.StartsWith("7.")) {
    $target_framework = "net7.0"
}
else {
    Write-Output "BUILD FAILURE: .NET 6, .NET 7 SDK required to run build"
    exit 1
}

dotnet run --project build/build.csproj -f $target_framework -c Release -- $args
