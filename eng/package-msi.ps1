param(
    [string]$Version = "0.1.0",
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$PublishRoot = Join-Path $RepoRoot "publish\msi"
$PayloadRoot = Join-Path $PublishRoot "payload"
$WebPayloadRoot = Join-Path $PayloadRoot "web"
$WixWorkRoot = Join-Path $PublishRoot "wix"
$GeneratedFilesWxs = Join-Path $WixWorkRoot "PublishedFiles.wxs"
$ArtifactsRoot = Join-Path $RepoRoot "artifacts\installer"
$MsiPath = Join-Path $ArtifactsRoot "NanoBot-$Version-$RuntimeIdentifier.msi"

function Remove-DirectoryInsideRepo([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $repoPath = [System.IO.Path]::GetFullPath($RepoRoot)
    if (-not $fullPath.StartsWith($repoPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside repository: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
}

function Convert-ToWixId([string]$Prefix, [string]$Value) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha256.ComputeHash($bytes)
    } finally {
        $sha256.Dispose()
    }
    $hex = -join ($hash[0..15] | ForEach-Object { $_.ToString("x2") })
    return "$Prefix$hex"
}

function Convert-ToStableGuid([string]$Value) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes("Nong.NanoBot.Net MSI component: $Value")
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $hash = $md5.ComputeHash($bytes)
    } finally {
        $md5.Dispose()
    }
    return (New-Object System.Guid -ArgumentList @(,$hash)).ToString("B").ToUpperInvariant()
}

function Escape-XmlAttribute([string]$Value) {
    return [System.Security.SecurityElement]::Escape($Value)
}

function Get-RelativePath([string]$BasePath, [string]$Path) {
    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath)
    if (-not $baseFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseFullPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $pathFullPath = [System.IO.Path]::GetFullPath($Path)
    $baseUri = New-Object System.Uri $baseFullPath
    $pathUri = New-Object System.Uri $pathFullPath
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace("/", "\").Replace("\", "/")
}

function New-WixFilesFragment {
    param(
        [string]$SourceRoot,
        [string]$OutputPath
    )

    $directories = Get-ChildItem -LiteralPath $SourceRoot -Directory -Recurse |
        Sort-Object FullName
    $files = Get-ChildItem -LiteralPath $SourceRoot -File -Recurse |
        Sort-Object FullName

    $directoryIdByRelativePath = @{}
    $directoryIdByRelativePath[""] = "INSTALLFOLDER"

    foreach ($directory in $directories) {
        $relativePath = Get-RelativePath $SourceRoot $directory.FullName
        $directoryIdByRelativePath[$relativePath] = Convert-ToWixId "dir_" $relativePath
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    $lines.Add('  <Fragment>')

    $childrenByParent = @{}
    foreach ($directory in $directories) {
        $relativePath = Get-RelativePath $SourceRoot $directory.FullName
        $parentRelativePath = Split-Path $relativePath -Parent
        if ($parentRelativePath -eq ".") {
            $parentRelativePath = ""
        } else {
            $parentRelativePath = $parentRelativePath.Replace("\", "/")
        }

        if (-not $childrenByParent.ContainsKey($parentRelativePath)) {
            $childrenByParent[$parentRelativePath] = [System.Collections.Generic.List[object]]::new()
        }

        $childrenByParent[$parentRelativePath].Add($directory)
    }

    function Write-DirectoryTree([string]$ParentRelativePath, [int]$Depth) {
        if (-not $childrenByParent.ContainsKey($ParentRelativePath)) {
            return
        }

        foreach ($child in $childrenByParent[$ParentRelativePath]) {
            $relativePath = Get-RelativePath $SourceRoot $child.FullName
            $id = $directoryIdByRelativePath[$relativePath]
            $name = Escape-XmlAttribute $child.Name
            $indent = " " * $Depth
            $lines.Add("$indent<Directory Id=""$id"" Name=""$name"">")
            Write-DirectoryTree $relativePath ($Depth + 2)
            $lines.Add("$indent</Directory>")
        }
    }

    $lines.Add('    <DirectoryRef Id="INSTALLFOLDER">')
    Write-DirectoryTree "" 6
    $lines.Add('    </DirectoryRef>')
    $lines.Add('  </Fragment>')

    $lines.Add('  <Fragment>')
    foreach ($file in $files) {
        $relativePath = Get-RelativePath $SourceRoot $file.FullName
        $parentRelativePath = Split-Path $relativePath -Parent
        if ($parentRelativePath -eq ".") {
            $parentRelativePath = ""
        } else {
            $parentRelativePath = $parentRelativePath.Replace("\", "/")
        }

        $directoryId = $directoryIdByRelativePath[$parentRelativePath]
        $componentId = Convert-ToWixId "cmp_" $relativePath
        $fileId = Convert-ToWixId "fil_" $relativePath
        $guid = Convert-ToStableGuid $relativePath
        $source = '$(var.PayloadDir)\' + $relativePath.Replace("/", "\")
        $escapedSource = Escape-XmlAttribute $source
        $lines.Add("    <DirectoryRef Id=""$directoryId"">")
        $lines.Add("      <Component Id=""$componentId"" Guid=""$guid"">")
        $lines.Add("        <File Id=""$fileId"" Source=""$escapedSource"" KeyPath=""yes"" />")
        $lines.Add('      </Component>')
        $lines.Add('    </DirectoryRef>')
    }
    $lines.Add('  </Fragment>')

    $lines.Add('  <Fragment>')
    $lines.Add('    <ComponentGroup Id="PublishedFiles">')
    foreach ($file in $files) {
        $relativePath = Get-RelativePath $SourceRoot $file.FullName
        $componentId = Convert-ToWixId "cmp_" $relativePath
        $lines.Add("      <ComponentRef Id=""$componentId"" />")
    }
    $lines.Add('    </ComponentGroup>')
    $lines.Add('  </Fragment>')
    $lines.Add('</Wix>')

    Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
}

Remove-DirectoryInsideRepo $PublishRoot
New-Item -ItemType Directory -Path $PayloadRoot, $WebPayloadRoot, $WixWorkRoot, $ArtifactsRoot -Force | Out-Null

dotnet publish (Join-Path $RepoRoot "Nanobot.CLI\Nanobot.CLI.csproj") `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -o $PayloadRoot

dotnet publish (Join-Path $RepoRoot "Nanobot.Web\Nanobot.Web.csproj") `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -o $WebPayloadRoot

Copy-Item -LiteralPath (Join-Path $RepoRoot "LICENSE") -Destination (Join-Path $PayloadRoot "LICENSE.txt")
Copy-Item -LiteralPath (Join-Path $RepoRoot "README.md") -Destination (Join-Path $PayloadRoot "README.md")
Copy-Item -LiteralPath (Join-Path $RepoRoot "README.zh-CN.md") -Destination (Join-Path $PayloadRoot "README.zh-CN.md")

New-WixFilesFragment -SourceRoot $PayloadRoot -OutputPath $GeneratedFilesWxs

dotnet tool restore
dotnet tool run wix -- build `
    (Join-Path $RepoRoot "installer\NanoBot.wxs") `
    $GeneratedFilesWxs `
    -arch x64 `
    -d "ProductVersion=$Version" `
    -d "PayloadDir=$PayloadRoot" `
    -o $MsiPath

if ($LASTEXITCODE -ne 0) {
    throw "WiX build failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $MsiPath)) {
    throw "WiX build finished without creating MSI: $MsiPath"
}

Write-Host "MSI created: $MsiPath"
