 .\scripts\Deploy-Stage.ps1 `
    -StageComputerName "161.132.56.79" `
    -ApiDestination "C:\Publish\Alas\api" `
    -FrontendDestination "C:\Publish\Alas\frontend" `
    -ApiAppPool "alasglobaltour.gestionaminegocio.com" `
    -FrontendAppPool "alasglobaltour.gestionaminegocio.com" `
    -FrontendService "AlasfrontendSSR"
	
	//Para generar y revisar los artefactos locales
	
	.\scripts\Deploy-Stage.ps1 `
    -StageComputerName "x" `
    -ApiDestination "x" `
    -FrontendDestination "x" `
    -PackageOnly `
	-SkipNpmCi