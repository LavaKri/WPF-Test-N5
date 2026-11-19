// =====================================================================================================
// CorrectifBarreEtat.cs — correctif réutilisable : NVDA+Fin ne trouve pas la StatusBar d'une fenêtre WPF.
//
// AUCUNE DÉPENDANCE : WPF seulement.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUT CORRIGÉ : NVDA ne cherche pas la barre d'état dans l'arbre UIA. api.getStatusBar() prend l'objet
//   situé au point (gauche, bas - 1) du rectangle de la fenêtre au premier plan, puis remonte ses parents
//   jusqu'à un rôle « barre d'état » (https://github.com/nvaccess/nvda, source/api.py).
//   Le rectangle UIA d'une fenêtre WPF est celui de la fenêtre entière, cadre compris : bordure invisible
//   de 8 px en fenêtre normale, débord hors de l'écran en fenêtre maximisée. Ce point tombe donc sur la
//   fenêtre elle-même et NVDA annonce qu'il n'y a pas de barre d'état.
//   Le correctif annonce comme rectangle UIA de la fenêtre celui de sa zone cliente : le coin inférieur
//   gauche tombe alors sur une StatusBar collée au bas de la zone cliente (DockPanel.Dock="Bottom").
//   Ce rectangle est limité à la zone de travail de l'écran : une fenêtre restaurée peut déborder sous la
//   barre des tâches, qui cacherait alors le point visé.
//   Vérifié par relevé UIA (AutomationElement.FromPoint), fenêtre maximisée et normale, .NET 10, novembre 2026.
//   Confirmé avec NVDA en fenêtre maximisée.
//
// EFFET DE BORD : quand le correctif est actif, le rectangle UIA de la fenêtre n'inclut plus la barre de
//   titre ni les bordures, ni la partie hors de la zone de travail.
//
// UTILISATION, dans la fenêtre qui porte la StatusBar :
//   protected override AutomationPeer OnCreateAutomationPeer()
//   {
//       return new CorrectifsWpf.CorrectifBarreEtat(this);
//   }
//   CorrectifsWpf.CorrectifBarreEtat.Actif = true;   // false : rectangle d'origine (comparaison avec et sans)
// =====================================================================================================

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Interop;

namespace CorrectifsWpf
{
    /// <summary>Peer de fenêtre dont le rectangle UIA est celui de la zone cliente, pour que NVDA+Fin trouve la StatusBar.</summary>
    public class CorrectifBarreEtat : WindowAutomationPeer
    {
        /// <summary>Faux : comportement d'origine de WPF. Lu à chaque demande du rectangle.</summary>
        public static bool Actif { get; set; }

        public CorrectifBarreEtat(Window fenetre) : base(fenetre)
        {
        }

        protected override Rect GetBoundingRectangleCore()
        {
            if (!Actif)
            {
                return base.GetBoundingRectangleCore();
            }

            IntPtr hwnd = new WindowInteropHelper((Window)Owner).Handle;
            RECT client;
            if (hwnd == IntPtr.Zero || !GetClientRect(hwnd, out client) || client.Right <= 0 || client.Bottom <= 0)
            {
                // Fenêtre pas encore créée ou réduite : rien de mieux que le rectangle d'origine.
                return base.GetBoundingRectangleCore();
            }

            // Coordonnées écran en pixels physiques, comme les attend UI Automation.
            POINT origine = new POINT();
            if (!ClientToScreen(hwnd, ref origine))
            {
                return base.GetBoundingRectangleCore();
            }
            Rect zone = new Rect(origine.X, origine.Y, client.Right, client.Bottom);

            // Fenêtre restaurée qui déborde sous la barre des tâches (ou hors de l'écran) : le bas de la zone
            // cliente est caché et le point visé par NVDA tomberait sur la barre des tâches. On s'en tient à
            // la partie visible, dans la zone de travail de l'écran de la fenêtre.
            MONITORINFO ecran = new MONITORINFO();
            ecran.cbSize = Marshal.SizeOf<MONITORINFO>();
            IntPtr moniteur = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (moniteur != IntPtr.Zero && GetMonitorInfo(moniteur, ref ecran))
            {
                Rect travail = new Rect(ecran.rcWork.Left, ecran.rcWork.Top,
                                        ecran.rcWork.Right - ecran.rcWork.Left, ecran.rcWork.Bottom - ecran.rcWork.Top);
                Rect visible = Rect.Intersect(zone, travail);
                if (!visible.IsEmpty && visible.Width > 0 && visible.Height > 0)
                {
                    zone = visible;
                }
            }
            return zone;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X, Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    }
}
