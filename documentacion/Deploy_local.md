 .\scripts\Deploy-Stage.ps1 `
    -StageComputerName "161.132.56.79" `
    -ApiDestination "C:\Publish\Alas\api" `
    -FrontendDestination "C:\Publish\Alas\frontend" `
    -ApiAppPool "alasglobaltour.gestionaminegocio.com" `
    -FrontendAppPool "alasglobaltour.gestionaminegocio.com" `
    -FrontendService "AlasfrontendSSR"
	
	//Para generar y revisar los artefactos locales en powershell 7
	cd c:\repo\rtres-net\alas
	
	.\scripts\Deploy-Stage.ps1 `
    -StageComputerName "x" `
    -ApiDestination "x" `
    -FrontendDestination "x" `
    -PackageOnly `
	-SkipNpmCi
	
	
	//En el servidor
	Stop-Service -Name "AlasfrontendSSR"

	# Copiar la nueva publicación:
	# frontend\browser
	# frontend\server

	Start-Service -Name "AlasfrontendSSR"