using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace RunCat365
{
    public partial class FloatingWindow : Window
    {
        public FloatingWindow()
        {
            InitializeComponent();

            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 50;
            Top = screen.Bottom - Height - 50;
        }

        public void UpdateIcon(Icon icon)
        {
            if (icon is null) return;

            var bitmap = icon.ToBitmap();
            
            if (Width == 48 && Height == 48)
            {
                Width = bitmap.Width;
                Height = bitmap.Height;
            }

            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            
            bitmap.Dispose();
            
            IconImage.Source = bitmapSource;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}
