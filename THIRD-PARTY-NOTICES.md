# Third-party notices

Ce projet est sous licence MIT (voir [`LICENSE`](LICENSE)). Il reprend du code d'un autre projet et utilise une bibliothèque tierce.

*This project is MIT-licensed (see `LICENSE`). It adapts code from another project and uses a third-party library.*

## Accessibility Insights for Windows

Des portions de `Correctifs/CorrectifArbreCases.cs` et de `Correctifs/CorrectifGrille.cs` sont adaptées d'[Accessibility Insights for Windows](https://github.com/microsoft/accessibility-insights-windows) (dossier `SharedUx/Controls/CustomControls`).

```
Accessibility Insights for Windows

Copyright (c) Microsoft Corporation
All rights reserved.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Harmony

Les correctifs marqués « Harmony » dans le [`README.md`](README.md) utilisent la bibliothèque [Harmony](https://github.com/pardeike/Harmony) (paquet NuGet `Lib.Harmony`, version 2.4.3), sous licence MIT, Copyright (c) 2017 Andreas Pardeike. Elle n'est pas incluse dans ce dépôt : elle est téléchargée à la compilation. Si vous distribuez un exécutable qui l'embarque (`0Harmony.dll`), joignez-y sa licence ([`LICENSE`](https://github.com/pardeike/Harmony/blob/master/LICENSE)).
