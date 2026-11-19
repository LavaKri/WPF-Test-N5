# Relevé UI Automation d'AccessTest : un bloc par onglet (éléments de la vue "contrôle").
# Usage : .\Audit-UIA.ps1 -Exe .\Net10\Debug\AccessTest.exe [-Sortie releve.txt]   (ou .\Net48\Debug\AccessTest.exe)
param(
    [Parameter(Mandatory)] [string] $Exe,
    [string] $Sortie,
    [switch] $Detail   # ajoute focalisation clavier, texte d'aide et patterns UIA pris en charge
)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$ignores = "TabItem", "Tab", "Pane", "ScrollBar", "Thumb", "TitleBar", "Window"

$p = Start-Process -FilePath (Resolve-Path $Exe) -PassThru
Start-Sleep -Seconds 4
$lignes = @()
try {
    $root = $AE::FromHandle($p.MainWindowHandle)
    $lignes += "Titre : " + $p.MainWindowTitle
    $ctl = New-Object System.Windows.Automation.PropertyCondition($AE::IsControlElementProperty, $true)
    $tabCond = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)
    foreach ($t in $root.FindAll($TS::Descendants, $tabCond)) {
        $t.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        Start-Sleep -Milliseconds 700
        $lignes += "=== " + $t.Current.Name
        foreach ($e in $root.FindAll($TS::Descendants, $ctl)) {
            $type = $e.Current.ControlType.ProgrammaticName.Replace("ControlType.", "")
            if ($type -in $ignores) { continue }
            $v = $e.GetCurrentPropertyValue($AE::LabeledByProperty, $true)
            $lb = if ([object]::ReferenceEquals($v, $AE::NotSupported)) { "" } else { " LabeledBy=oui" }
            $suite = ""
            if ($Detail) {
                $patterns = ($e.GetSupportedPatterns() | ForEach-Object { $_.ProgrammaticName.Replace("Identifiers.Pattern", "") } | Sort-Object) -join ","
                $suite = " | focus={0} aide=[{1}] patterns={2}" -f $e.Current.IsKeyboardFocusable, $e.Current.HelpText, $patterns
            }
            $lignes += ("{0,-11} [{1}]{2}{3}" -f $type, $e.Current.Name, $lb, $suite)
        }
    }
}
finally {
    $p.CloseMainWindow() | Out-Null
    Start-Sleep 1
    if (-not $p.HasExited) { $p.Kill() }
}

if ($Sortie) { $lignes | Set-Content $Sortie -Encoding utf8 } else { $lignes }
