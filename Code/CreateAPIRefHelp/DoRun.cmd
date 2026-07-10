rem Copy the TLBs from the build folder to C:\EnterpriseHelp\TLB
copy "\\archive2.esri.com\ProBuilds\main\2865\Debug\ArcGIS\com\server\*.tlb" "C:\Repos\Wolf-K\EnterpriseSDK\TLB" /Y
copy "\\archive2.esri.com\ProBuilds\main\2865\Release\ArcGIS\DotNet\ProSOE\*.dll" "C:\Repos\Wolf-K\EnterpriseSDK\TLB" /Y
rem make the XML files needed for DocFXsave into the "C:\Repos\Wolf-K\API-Doc\bin" folder
"c:\tools\CreateAPIRefHelp\CreateAPIRefHelp.exe" "C:\Repos\Wolf-K\EnterpriseSDK\TLB" "C:\Repos\Wolf-K\EnterpriseSDK\bin" ESRI.Server 12.2 sample-include.json /xml /clean