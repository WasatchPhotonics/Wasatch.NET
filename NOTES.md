# Overview

Maintenance notes for ongoing development of Wasatch.NET.

# Post-Build Event

Consider something like the following?

<code>
if "$(Platform)" == "x64" (
    echo "Exporting 64-bit TLB (assuming host OS is 64-bit)"
    "$(FrameworkSdkDir)bin\NetFX 4.6.1 Tools\tlbexp.exe" $(TargetDir)$(ProjectName).dll
) else (
    echo Exporting 32-bit TLB
    "$(FrameworkSdkDir)bin\NetFX 4.6.1 Tools\tlbexp.exe" $(TargetDir)$(ProjectName).dll /Win32
)

copy /Y "$(TargetDir)$(ProjectName).*" "$(SolutionDir)lib\$(Platform)\"
</code>
