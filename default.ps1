Properties {
    $rootDir = ".\"
    $slnPath = "$rootDir\src\github-tools.sln"
    $outDir = "$rootDir\build\"
}

TaskSetup {
    TeamCity-ReportBuildProgress "Running task $($psake.context.Peek().currentTaskName)"
}

task default -depends set-version,rebuild,copy-output

task rebuild -depends clean,build

task set-version {
    $gittag = (& git describe --long)
    if ($gittag -match '-(\d+)-') {
        $gitnumber = $matches[1]
    } else {
        $gitnumber = 0
    }
    $tc_build_number = "$env:BUILD_NUMBER-$gitnumber"
    TeamCity-SetBuildNumber $tc_build_number
}

task clean {
    Write-Host "Creating build output directory" -ForegroundColor Green
    if (Test-Path $outDir) {
        rd $outDir -rec -force | out-null
    }
    mkdir $outDir | out-null

    Write-Host "Cleaning solution" -ForegroundColor Green
    exec { msbuild $slnPath '/t:Clean' '/v:quiet' }
}

task build { 
    exec { msbuild "$slnPath" /t:Build /v:minimal }
}

task copy-output {
    mkdir $outDir\hooks | out-null
    Copy-Item .\src\hooks\Web.config $outDir\hooks\Web.config
    Copy-Item .\src\hooks\bin $outDir\hooks -recurse
    Copy-Item .\src\hooks\Views $outDir\hooks -recurse

    $zipFile = "$outDir\output.zip"
    Invoke-Expression -Command "$rootDir\tools\7zip\7z.exe a $zipFile $outDir\*"
    TeamCity-PublishArtifact $zipFile
}