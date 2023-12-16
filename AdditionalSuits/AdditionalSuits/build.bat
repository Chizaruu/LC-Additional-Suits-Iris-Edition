dotnet build %~dp0IrisChansAdditonalSuits.csproj
powershell -Command Copy-Item -Force -Path "%~dp0bin\Debug\IrisChansAdditonalSuits.dll" -Destination "%~dp0thunderstore\IrisChansAdditonalSuits.dll"
