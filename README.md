# Aardvark.Cef.Runtime
[![Publish](https://github.com/aardvark-community/aardvark.cef.runtime/actions/workflows/publish.yml/badge.svg)](https://github.com/aardvark-community/aardvark.cef.runtime/actions/workflows/publish.yml)
[![Version](https://img.shields.io/nuget/vpre/aardvark.cef.runtime)](https://www.nuget.org/packages/aardvark.cef.runtime/)
[![Downloads](https://img.shields.io/nuget/dt/aardvark.cef.runtime)](https://www.nuget.org/packages/aardvark.cef.runtime/)

Native CEF runtimes for the Aardvark platform.

## Usage
Reference this package to automatically copy the appropriate CEF runtime to the output folder when building your project.

Modify the version number in `VERSION.txt` and commit the changes. The CI workflow will download the corresponding runtimes from https://cef-builds.spotifycdn.com/index.html and publish a new package. You can also run `pack.cmd` to create a package locally.
