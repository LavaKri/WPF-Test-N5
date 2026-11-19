# AccessTest

Application WPF qui sert à vérifier, avec un lecteur d'écran (NVDA, JAWS), l'accessibilité des contrôles standard de WPF, sur .NET 10 et sur .NET Framework 4.8. Elle propose aussi des **correctifs réutilisables** pour des défauts d'accessibilité de WPF.

*English summary: a WPF test application for checking how screen readers (NVDA, JAWS) read the standard WPF controls, built for .NET 10 and .NET Framework 4.8 from the same sources. The `Correctifs/` folder holds reusable, single-file fixes for known WPF accessibility defects. Documentation is in French.*

## Prérequis : 
- Windows et le SDK .NET 10 (`winget install Microsoft.DotNet.SDK.10`). 
- Ce SDK suffit pour compiler aussi la cible .NET Framework 4.8, sans Visual Studio.

## Contributions

Ce dépôt est publié tel quel, en lecture seule : les issues, les pull requests et les commentaires ne sont pas pris en compte, et aucune contribution ne sera examinée ni fusionnée. Vous pouvez copier, adapter et réutiliser le code librement, selon la licence MIT.

*This repository is published as is. Issues, pull requests and comments are not monitored and contributions will not be merged. You are free to fork and reuse the code under the MIT license.*

## Licence

[MIT](LICENSE). Des portions de `CorrectifArbreCases` et `CorrectifGrille` sont adaptées d'[Accessibility Insights for Windows](https://github.com/microsoft/accessibility-insights-windows) (Microsoft, licence MIT) : voir [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
