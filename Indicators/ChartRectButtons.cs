

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace ChartRectButtons
{

    public class ChartRectButtons : Indicator
    {
        private Point lastMousePoint = Point.Empty;

        private bool AllowLongs = false;
        private bool AllowShorts = false;

        private Rectangle LongRect = new Rectangle(20, 60, 100, 30);
        private int LongStringX = 40;
        private int LongStringY = 60;
        private String LongString = "Long";
        private int LongFontSize = 18;

        private Rectangle ShortRect = new Rectangle(20, 100, 100, 30);
        private int ShortStringX = 40;
        private int ShortStringY = 100;
        private String ShortString = "Short";
        private int ShortFontSize = 18;


        public ChartRectButtons()
            : base()
        {
            // Defines indicator's name and description.
            Name = "ChartRectButtons";
            Description = "My indicator's annotation";

            // Defines line on demand with particular parameters.
            AddLineSeries("line1", Color.CadetBlue, 1, LineStyle.Solid);

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }


        protected override void OnInit()
        {
            // Add your initialization code here

            this.CurrentChart.MouseClick += CurrentChart_MouseClick;
        }

        private void CurrentChart_MouseClick(object sender, TradingPlatform.BusinessLayer.Chart.ChartMouseNativeEventArgs e)
        {
            if (e.Button == TradingPlatform.BusinessLayer.Native.NativeMouseButtons.Left && this.CurrentChart.MainWindow.ClientRectangle.Contains(e.Location))
            {
                this.lastMousePoint = e.Location;
                if (LongRect.Contains(this.lastMousePoint))
                {
                    AllowLongs = !AllowLongs;
                    e.Handled = true;
                }
                else if (ShortRect.Contains(this.lastMousePoint))
                {
                    AllowShorts = !AllowShorts;
                    e.Handled = true;
                }
            }
        }

        protected override void OnUpdate(UpdateArgs args)
        {

        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            Graphics gr = args.Graphics;

            Brush enabledBrush = Brushes.Green;
            Brush disabledBrush = Brushes.Red;

            Brush longBrush = AllowLongs ? enabledBrush : disabledBrush;
            Brush shortBrush = AllowShorts ? enabledBrush : disabledBrush;


            gr.FillRectangle(longBrush, LongRect);
            gr.FillRectangle(shortBrush, ShortRect);


            gr.DrawString(LongString, new Font("Arial", LongFontSize), Brushes.White, LongStringX, LongStringY);
            gr.DrawString(ShortString, new Font("Arial", ShortFontSize), Brushes.White, ShortStringX, ShortStringY);


            if (lastMousePoint != Point.Empty)
            {
                gr.DrawString($"x: {lastMousePoint.X} y: {lastMousePoint.Y}", new Font("Arial", 20), Brushes.Red, 30, 150);
            }

        }

        public override void Dispose()
        {
            if (this.CurrentChart != null)
                this.CurrentChart.MouseClick -= CurrentChart_MouseClick;

            base.Dispose();
        }
    }
}
