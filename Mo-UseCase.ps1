<#
Mo-UseCase.ps1
Menu dieu huong nhanh den code cho day du UC-A1 den UC-A8 cua Smart Tour Guide.
Script chi doc source, tu tim dong hien tai va mo file tai dong tuong ung.
Khong sua repository, khong tao bookmark trong IDE, khong commit/push.

Cach dung:
  1. Dat file nay trong thu muc goc smart-tour-guide-backend_fix.
  2. Mo PowerShell tai thu muc goc solution.
  3. Chay: .\Mo-UseCase.ps1
  4. Chon UC, sau do chon vi tri can mo.

IDE:
  .\Mo-UseCase.ps1 -IDE Auto
  .\Mo-UseCase.ps1 -IDE VisualStudio
  .\Mo-UseCase.ps1 -IDE VSCode

Auto uu tien Visual Studio dang mo qua DTE; neu khong duoc thi thu VS Code CLI.
#>

param(
    [ValidateSet('Auto', 'VisualStudio', 'VSCode')]
    [string]$IDE = 'Auto'
)

$ErrorActionPreference = 'Stop'
$Utf8 = [System.Text.UTF8Encoding]::new($false)

function Get-RepositoryRoot {
    $candidate = (Get-Location).Path
    while ($candidate) {
        if ((Test-Path (Join-Path $candidate 'SmartTourGuide.API')) -and
            (Test-Path (Join-Path $candidate 'SmartTourGuide.sln'))) {
            return $candidate
        }
        $parent = Split-Path $candidate -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) { break }
        $candidate = $parent
    }

    $scriptRoot = $PSScriptRoot
    if ((Test-Path (Join-Path $scriptRoot 'SmartTourGuide.API')) -and
        (Test-Path (Join-Path $scriptRoot 'SmartTourGuide.sln'))) {
        return $scriptRoot
    }

    throw 'Khong tim thay thu muc goc solution. Hay dat script trong thu muc smart-tour-guide-backend_fix.'
}

$Root = Get-RepositoryRoot

function Find-Lines {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Label,
        [int]$MaxMatches = 0
    )

    $fullPath = Join-Path $Root $RelativePath
    if (-not (Test-Path $fullPath)) { return }

    $lines = [System.IO.File]::ReadAllLines($fullPath, $Utf8)
    $count = 0
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match $Pattern) {
            [PSCustomObject]@{
                Label = $Label
                RelativePath = $RelativePath
                FullPath = $fullPath
                Line = $i + 1
                Preview = $lines[$i].Trim()
            }
            $count++
            if ($MaxMatches -gt 0 -and $count -ge $MaxMatches) { break }
        }
    }
}

function New-Group {
    param([string]$Name)
    return [PSCustomObject]@{
        Name = $Name
        Items = [System.Collections.Generic.List[object]]::new()
    }
}

function Add-FoundItems {
    param($Group, [object[]]$Found)
    foreach ($item in $Found) {
        if ($null -ne $item) { $Group.Items.Add($item) }
    }
}

function Get-Targets {
    $targets = [ordered]@{}

    # UC-A1: Mo tour qua QR va nhan payload tu Public API.
    $g = New-Group 'UC-A1 - Truy cap tour bang QR'
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\PublicToursController.cs' '\[HttpGet\s*\(\s*"qr/\{accessCode\}"' 'Backend - GET QR')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\index.html' 'async\s+function\s+loadTour\s*\(' 'Frontend - loadTour()')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\PublicToursController.cs' 'ExecuteUpdateAsync|QrCodeType\.SingleUse' 'Backend - claim luot dung / SingleUse' 2)
    $targets['1'] = $g

    # UC-A2: Xem danh sach POI va noi dung ban dich.
    $g = New-Group 'UC-A2 - Xem POI va ban dich'
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\PublicToursController.cs' 'GetByQrCode\s*\(' 'Backend - response tour/POI/translation')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\index.html' 'function\s+renderTour\s*\(' 'Frontend - renderTour()')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\index.html' 'function\s+showLocationDetail\s*\(' 'Frontend - showLocationDetail()')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Context\SmartTourGuideDbContext.cs' 'TourLocationTranslations' 'Data - DbSet TourLocationTranslations')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Entities\TourLocationTranslation.cs' 'class\s+TourLocationTranslation' 'Data - entity ban dich POI')
    $targets['2'] = $g

    # UC-A3: Heartbeat va ket thuc phien khach.
    $g = New-Group 'UC-A3 - Ket thuc phien khach'
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\PublicToursController.cs' '\[HttpPost\s*\(\s*"sessions/\{sessionId:guid\}/heartbeat"' 'Backend - POST heartbeat')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\PublicToursController.cs' '\[HttpPost\s*\(\s*"sessions/\{sessionId:guid\}/end"' 'Backend - POST end session')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\index.html' 'navigator\.sendBeacon|addEventListener\s*\(\s*.*pagehide' 'Frontend - gui yeu cau ket thuc phien')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\index.html' 'function\s+startGuestTracking|guestHeartbeatTimer' 'Frontend - theo doi phien/heartbeat' 2)
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Entities\TourSession.cs' 'class\s+TourSession|Status|EndedAt|LastActivityAt' 'Data - entity TourSession' 4)
    $targets['3'] = $g

    # UC-A4: Dang ky, dang nhap, doc ho so Visitor.
    $g = New-Group 'UC-A4 - Tai khoan Visitor'
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\AuthController.cs' '\[HttpPost\s*\(\s*"register"' 'API - POST register')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\AuthController.cs' '\[HttpPost\s*\(\s*"login"' 'API - POST login')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\AuthController.cs' '\[HttpGet\s*\(\s*"me"' 'API - GET me')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Business\Services\AuthService.cs' 'RegisterAsync\s*\(' 'Business - dang ky')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Business\Services\AuthService.cs' 'LoginAsync\s*\(' 'Business - dang nhap')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Business\Services\TokenService.cs' 'CreateRegisteredUserToken\s*\(' 'Business - tao JWT')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Repositories\UserRepository.cs' 'class\s+UserRepository|ConcurrentDictionary|AddAsync\s*\(' 'Data - luu tai khoan Visitor' 3)
    $targets['4'] = $g

    # UC-A5: Quan ly tour, POI va translations qua Admin API.
    $g = New-Group 'UC-A5 - Quan ly tour, POI va ban dich'
    $admin = 'SmartTourGuide.API\Controllers\AdminController.cs'
    Add-FoundItems $g (Find-Lines $admin '\[HttpGet\s*\(\s*"tours"' 'API - GET danh sach tour')
    Add-FoundItems $g (Find-Lines $admin '\[HttpGet\s*\(\s*"tours/\{id:guid\}"' 'API - GET chi tiet tour')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPost\s*\(\s*"tours"' 'API - POST tao tour')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPut\s*\(\s*"tours/\{id:guid\}"' 'API - PUT cap nhat tour')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPost\s*\(\s*"tours/\{tourId:guid\}/locations"' 'API - POST tao POI va ban dich')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPut\s*\(\s*"locations/\{id:guid\}"' 'API - PUT cap nhat POI/ban dich')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPatch\s*\(\s*"locations/\{id:guid\}/active"' 'API - PATCH trang thai POI')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPatch\s*\(\s*"tours/\{id:guid\}/active"' 'API - PATCH trang thai tour')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Context\SmartTourGuideDbContext.cs' 'TourLocations|TourLocationTranslations|TourQrCodes' 'Data - DbSet du lieu tour/POI' 3)
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Entities\TourLocationTranslation.cs' 'class\s+TourLocationTranslation' 'Data - entity ban dich POI')
    $targets['5'] = $g

    # UC-A6: Quan ly ma QR rieng.
    $g = New-Group 'UC-A6 - Quan ly ma QR'
    Add-FoundItems $g (Find-Lines $admin '\[HttpGet\s*\(\s*"qrcodes"' 'API - GET danh sach QR')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPost\s*\(\s*"qrcodes"' 'API - POST tao QR')
    Add-FoundItems $g (Find-Lines $admin '\[HttpPatch\s*\(\s*"qrcodes/\{id:guid\}/deactivate"' 'API - PATCH vo hieu hoa QR')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\admin\index.html' 'function\s+renderQr\s*\(' 'Frontend - tao anh QR demo co dinh (khong goi CRUD QR)')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Entities\TourQrCode.cs' 'class\s+TourQrCode|UsageCount|MaximumUsageCount|ExpiresAt|IsActive' 'Data - entity va gioi han QR' 5)
    $targets['6'] = $g

    # UC-A7: Dashboard va danh sach phien khach.
    $g = New-Group 'UC-A7 - Dashboard va phien khach gan day'
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\AdminDashboardController.cs' '\[HttpGet\s*\(\s*"overview"' 'API - GET overview')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\AdminDashboardController.cs' '\[HttpGet\s*\(\s*"sessions"' 'API - GET sessions')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Program.cs' 'AdminDashboardAccess' 'Policy - AdminDashboardAccess')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\admin\index.html' 'async\s+function\s+refresh\s*\(' 'Frontend - refresh()')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Context\SmartTourGuideDbContext.cs' 'TourSessions|GuestIdentities|TourQrCodes' 'Data - DbSet phuc vu dashboard' 3)
    $targets['7'] = $g

    # UC-A8: Reset so lieu thong ke.
    $g = New-Group 'UC-A8 - Reset thong ke dashboard'
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\Controllers\AdminDashboardController.cs' '\[HttpPost\s*\(\s*"overview/reset"' 'API - POST overview/reset')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.API\wwwroot\admin\index.html' 'resetStats.*addEventListener' 'Frontend - nut reset thong ke')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Entities\TourQrCode.cs' 'UsageCount' 'Data - reset UsageCount cua QR')
    Add-FoundItems $g (Find-Lines 'SmartTourGuide.Data\Entities\TourSession.cs' 'ExcludedFromGuestTotal' 'Data - danh dau phien khong tinh vao tong khach')
    $targets['8'] = $g

    return $targets
}

function Get-ActiveVisualStudioDte {
    param([string]$ProgId)

    # .NET Framework co Marshal.GetActiveObject; PowerShell 7 co the khong con API nay.
    try {
        $getActiveObject = [System.Runtime.InteropServices.Marshal].GetMethod(
            'GetActiveObject',
            [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static,
            $null,
            [type[]]@([string]),
            $null
        )
        if ($null -ne $getActiveObject) {
            return $getActiveObject.Invoke($null, @($ProgId))
        }
    }
    catch { }

    # Du phong cho PowerShell 7 tren Windows: goi truc tiep COM GetActiveObject.
    try {
        if (-not ('UseCaseActiveObjectHelper' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class UseCaseActiveObjectHelper
{
    [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int CLSIDFromProgID(string progId, out Guid clsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid clsid, IntPtr reserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object instance);

    public static object GetObject(string progId)
    {
        Guid clsid;
        int result = CLSIDFromProgID(progId, out clsid);
        if (result < 0) Marshal.ThrowExceptionForHR(result);

        object instance;
        result = GetActiveObject(ref clsid, IntPtr.Zero, out instance);
        if (result < 0) Marshal.ThrowExceptionForHR(result);
        return instance;
    }
}
'@
        }
        return [UseCaseActiveObjectHelper]::GetObject($ProgId)
    }
    catch { return $null }
}

function Open-InVisualStudio {
    param([string]$Path, [int]$Line)

    foreach ($progId in @('VisualStudio.DTE.18.0', 'VisualStudio.DTE.17.0', 'VisualStudio.DTE.16.0')) {
        try {
            $dte = Get-ActiveVisualStudioDte -ProgId $progId
            if ($null -ne $dte) {
                $null = $dte.ItemOperations.OpenFile($Path)
                $dte.ActiveDocument.Selection.GotoLine($Line, $true)
                $dte.MainWindow.Activate()
                return $true
            }
        }
        catch { }
    }
    return $false
}

function Open-InVSCode {
    param([string]$Path, [int]$Line)
    $codeCommand = Get-Command 'code' -ErrorAction SilentlyContinue
    if ($null -eq $codeCommand) { return $false }
    $location = '{0}:{1}:1' -f $Path, $Line
    & $codeCommand.Source --goto $location
    return ($LASTEXITCODE -eq 0 -or $null -eq $LASTEXITCODE)
}

function Open-Target {
    param($Target)

    Write-Host ''
    Write-Host ('Dang mo: {0} (dong {1})' -f $Target.RelativePath, $Target.Line) -ForegroundColor Cyan
    $opened = $false

    if ($IDE -in @('Auto', 'VisualStudio')) {
        $opened = Open-InVisualStudio -Path $Target.FullPath -Line $Target.Line
        if ($opened) { return }
        if ($IDE -eq 'VisualStudio') {
            Write-Warning 'Khong ket noi duoc Visual Studio dang mo qua DTE. Hay thu -IDE VSCode hoac mo bang duong dan hien ben duoi.'
        }
    }

    if ($IDE -in @('Auto', 'VSCode')) {
        $opened = Open-InVSCode -Path $Target.FullPath -Line $Target.Line
        if ($opened) { return }
        if ($IDE -eq 'VSCode') {
            Write-Warning 'Khong tim thay lenh code cua VS Code trong PATH.'
        }
    }

    Write-Host 'Mo thu cong theo duong dan/dong sau:' -ForegroundColor Yellow
    Write-Host ('{0}:{1}' -f $Target.FullPath, $Target.Line) -ForegroundColor Yellow
}

try {
    while ($true) {
        $groups = Get-Targets
        Clear-Host
        Write-Host '====================================================' -ForegroundColor DarkCyan
        Write-Host '       SMART TOUR GUIDE - DIEU HUONG UC-A1..UC-A8' -ForegroundColor Cyan
        Write-Host '====================================================' -ForegroundColor DarkCyan
        Write-Host ('Thu muc: {0}' -f $Root) -ForegroundColor Gray
        Write-Host ''
        Write-Host '1. UC-A1 - Truy cap tour bang QR'
        Write-Host '2. UC-A2 - Xem POI va ban dich'
        Write-Host '3. UC-A3 - Ket thuc phien khach'
        Write-Host '4. UC-A4 - Tai khoan Visitor'
        Write-Host '5. UC-A5 - Quan ly tour, POI va ban dich'
        Write-Host '6. UC-A6 - Quan ly ma QR'
        Write-Host '7. UC-A7 - Dashboard va phien khach gan day'
        Write-Host '8. UC-A8 - Reset thong ke dashboard'
        Write-Host '0. Thoat'
        Write-Host ''
        $choice = Read-Host 'Chon Use Case'
        if ($choice -eq '0') { break }
        if (-not $groups.Contains($choice)) { continue }

        $group = $groups[$choice]
        $items = @($group.Items)
        Clear-Host
        Write-Host $group.Name -ForegroundColor Cyan
        Write-Host '----------------------------------------------------'

        if ($items.Count -eq 0) {
            Write-Warning 'Khong tim thay diem mo trong source hien tai. Co the ten route/file da khac; hay gui lai thong bao nay.'
            [void](Read-Host 'Nhan Enter de quay lai')
            continue
        }

        for ($i = 0; $i -lt $items.Count; $i++) {
            Write-Host ('{0}. {1}' -f ($i + 1), $items[$i].Label) -ForegroundColor White
            Write-Host ('   {0}:{1}' -f $items[$i].RelativePath, $items[$i].Line) -ForegroundColor DarkGray
            Write-Host ('   {0}' -f $items[$i].Preview) -ForegroundColor Gray
        }

        Write-Host ''
        Write-Host '0. Quay lai menu chinh'
        $subChoice = Read-Host 'Chon vi tri can mo'
        if ($subChoice -eq '0') { continue }
        $selectedIndex = 0
        if (-not [int]::TryParse($subChoice, [ref]$selectedIndex)) { continue }
        if ($selectedIndex -lt 1 -or $selectedIndex -gt $items.Count) { continue }
        Open-Target -Target $items[$selectedIndex - 1]
        [void](Read-Host 'Nhan Enter de quay lai menu')
    }
}
catch {
    Write-Host ('Loi: {0}' -f $_.Exception.Message) -ForegroundColor Red
    throw
}
