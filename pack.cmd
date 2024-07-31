@echo off

dotnet tool restore
dotnet paket restore
dotnet fsi fetch.fsx --keep-downloads || exit /b
set /p Version=<VERSION.txt

for /f "tokens=1 delims=+" %%a in ("%Version%") do (
  set Version=%%a
  )

dotnet paket pack bin\pack --version %Version%