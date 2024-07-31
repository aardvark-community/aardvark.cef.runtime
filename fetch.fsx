#r "nuget: SharpZipLib"
#r "System.Formats.Tar.dll"

open System
open System.IO
open System.Net.Http
open System.Formats.Tar
open ICSharpCode.SharpZipLib.BZip2

let version = File.ReadAllText "VERSION.txt"

let platforms =
    Map.ofList [
        "win-x64", "windows64"
    ]

let content =
    [| "Release"; "Resources" |]

module Download =

    [<Struct>]
    type Progress(received : int64, total : int64) =
        member x.Received = received
        member x.Total = total
        member x.Relative = float received / float total

        override x.ToString() =
            sprintf "%.2f%%" (100.0 * float received / float total)

    let file (url: string) (file: string) =
        Console.WriteLine($"Retrieving {url}")

        try
            use c = new HttpClient()

            let response = c.GetAsync(System.Uri url, HttpCompletionOption.ResponseHeadersRead).Result
            let len =
                let f = response.Content.Headers.ContentLength
                if f.HasValue then f.Value
                else 1L <<< 30

            if not response.IsSuccessStatusCode then
                let code = response.StatusCode
                raise <| HttpRequestException($"Http GET request failed with status code {int code} ({code}).")

            let mutable lastProgress = Progress(0L,len)
            Console.WriteLine($"Downloading {len} bytes...")
            Console.WriteLine($"{lastProgress}")
            let sw = System.Diagnostics.Stopwatch.StartNew()

            use stream = response.Content.ReadAsStreamAsync().Result
            if File.Exists file then File.Delete file
            use output = File.OpenWrite(file)

            let buffer : byte[] = Array.zeroCreate (4 <<< 20)

            let mutable remaining = len
            let mutable read = 0L
            while remaining > 0L do
                let cnt = int (min remaining buffer.LongLength)
                let r = stream.Read(buffer, 0, cnt)
                output.Write(buffer, 0, r)

                remaining <- remaining - int64 r
                read <- read + int64 r

                let p = Progress(read, len)
                if sw.Elapsed.TotalSeconds >= 0.1 && p.Relative - lastProgress.Relative > 0.025 then
                    Console.WriteLine($"{p}")
                    lastProgress <- p
                    sw.Restart()

            Console.WriteLine($"Written {read} bytes to {file}")
        with _ ->
            Console.Error.WriteLine("FAILED")
            if File.Exists file then File.Delete file
            reraise()

module Directory =

    let rec copy (src: string) (dst: string) =
        let dir = DirectoryInfo(src)
        Directory.CreateDirectory dst |> ignore

        for f in dir.GetFiles() do
            f.CopyTo(Path.Combine(dst, f.Name), true) |> ignore

        for d in dir.GetDirectories() do
            let newDst = Path.Combine(dst, d.Name)
            copy d.FullName newDst


let keepDownloads =
    fsi.CommandLineArgs |> Array.contains "--keep-downloads"

try
    for KeyValue (rid, platform) in platforms do
        let name = $"cef_binary_{version}_{platform}_minimal.tar.bz2"
        let file = Path.Combine("download", name)
        let dir = file.Replace(".tar.bz2", "")

        if not <| Directory.Exists dir then
            Directory.CreateDirectory("download") |> ignore

            if not <| File.Exists file then
                let url = $"https://cef-builds.spotifycdn.com/cef_binary_{version}_{platform}_minimal.tar.bz2"
                Download.file url file

            Console.WriteLine($"Unpacking {file}...")

            use bz2Stream = File.OpenRead(file)
            use tarStream = new MemoryStream()
            BZip2.Decompress(bz2Stream, tarStream, false)

            tarStream.Seek(0L, SeekOrigin.Begin) |> ignore
            TarFile.ExtractToDirectory(tarStream, "download", true)

        let dst = Path.Combine("cef", rid)
        Directory.CreateDirectory(dst) |> ignore

        Console.WriteLine($"Copying to {dst}...")

        for c in content do
            let src = Path.Combine(dir, c)
            Directory.copy src dst

finally
    if not keepDownloads && Directory.Exists "download" then Directory.Delete("download", true)