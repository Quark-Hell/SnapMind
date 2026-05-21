using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapMind.Screenshoter
{
    internal class RegionSelectorForm : Form
    {
        private Point startPoint;
        private Rectangle selection;
        private bool isSelecting;

        public Rectangle SelectedRegion { get; private set; }

        public static Rectangle SelectRegion()
        {
            using var form = new RegionSelectorForm();

            if (form.ShowDialog() == DialogResult.OK)
                return form.SelectedRegion;

            return Rectangle.Empty;
        }

        public RegionSelectorForm()
        {
            DoubleBuffered = true;
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            BackColor = Color.Black;
            Opacity = 0.3;
            Cursor = Cursors.Cross;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            isSelecting = true;
            startPoint = e.Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!isSelecting) return;

            selection = new Rectangle(
                Math.Min(startPoint.X, e.X),
                Math.Min(startPoint.Y, e.Y),
                Math.Abs(startPoint.X - e.X),
                Math.Abs(startPoint.Y - e.Y));

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isSelecting = false;
            SelectedRegion = selection;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var pen = new Pen(Color.Red, 2);
            e.Graphics.DrawRectangle(pen, selection);
        }
    }
}
