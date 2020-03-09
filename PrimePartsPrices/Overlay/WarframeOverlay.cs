using GameOverlay.Drawing;
using GameOverlay.Windows;
using PrimePartsPrices.Entities;
using PrimePartsPrices.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace PrimePartsPrices.Overlay
{
    public class WarframeOverlay
    {
        private readonly GraphicsWindow _window;
        private readonly Process _process;
        private IEnumerable<PrimePart> _primeParts;

        private Font _font;

        private SolidBrush _redBrush;
        private SolidBrush _grayBrush;
        private SolidBrush _yellowBrush;
        private SolidBrush _whiteBrush;


        public WarframeOverlay(Process process)
        {
            _process = process;

            Graphics graphics = new Graphics
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = true,
                WindowHandle = _process.MainWindowHandle
            };

            _window = new StickyWindow(_process.MainWindowHandle, graphics)
            {
                IsTopmost = true,
                IsVisible = true,
                FPS = 30,
                X = 0,
                Y = 0,
                Width = 800,
                Height = 600
            };

            _window.SetupGraphics += _window_SetupGraphics;
            _window.DestroyGraphics += _window_DestroyGraphics;
            _window.DrawGraphics += _window_DrawGraphics;
        }

        /// <summary>
        /// Creates the window and setups the graphics
        /// </summary>
        public void Run(IEnumerable<PrimePart> primeParts)
        {
            _primeParts = primeParts;
            _window.StartThread();
        }

        /// <summary>
        /// Stops the overlay
        /// </summary>
        public void Stop()
        {
            _window.StopThreadAsync();
        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            _font = e.Graphics.CreateFont("Arial", 16);

            _redBrush = e.Graphics.CreateSolidBrush(Color.Red);
            _grayBrush = e.Graphics.CreateSolidBrush(192, 192, 192);
            _yellowBrush = e.Graphics.CreateSolidBrush(255, 255, 102);
            _whiteBrush = e.Graphics.CreateSolidBrush(255, 255, 255);
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            ProcessWindowHelper.GetWindowThreadProcessId(ProcessWindowHelper.GetForegroundWindow(), out int activeProcId);

            float xIndex = 10, yIndex = 10;

            if (activeProcId == _process.Id)
            {
                foreach (PrimePart primePart in _primeParts)
                {
                    e.Graphics.DrawText(_font, _whiteBrush, xIndex, yIndex, primePart.ToString());

                    yIndex += _font.FontSize + 5;
                }
            }
            else
            {
                e.Graphics.ClearScene();
            }
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            // you may want to dispose any brushes, fonts or images
            e.Graphics.BeginScene();
            e.Graphics.ClearScene();
            e.Graphics.EndScene();
            _font.Dispose();
            _redBrush.Dispose();
            _grayBrush.Dispose();
            _yellowBrush.Dispose();
            _whiteBrush.Dispose();
        }
    }
}
