using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Vision_OpenCV_App
{
    public enum DrawingMode
    {
        None,
        Roi,
        Line,
        Circle,
        Rectangle
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 이미지 이동을 위한 변수
        private Point _origin;      // 이동 시작 시점의 이미지 위치
        private Point _start;       // 이동 시작 시점의 마우스 위치
        private bool _isDragging = false;   // 현재 마우스를 끌고 있는지 여부

        // ROI 관련 변수
        private bool _isRoiDrawing = false; // 현재 ROI를 그리고 있는지 여부.
        private Point _roiStartPoint;       // 이미지 기준 좌표: ROI 사각형을 그리기 시작한 점 (클릭한 곳)
        private Rect _currentRoiRect;       // 최종적으로 계산된 ROI 영역 (X, Y, W, H)

        // 그리기 관련 변수
        private DrawingMode _currentDrawMode = DrawingMode.None;
        private Point _drawStartPoint;      // 그리기 시작점 (이미지 좌표)
        private Shape _tempShape;           // 그리기 도중 보여줄 임시 도형.

        public MainWindow()
        {
            InitializeComponent();

            //var ViewModel = new MainViewModel();
            //this.DataContext = ViewModel;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = this.DataContext as MainViewModel;
            vm?.Cleanup();
        }

        private void ZoomBorder_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => FitImageToScreen(), DispatcherPriority.ContextIdle);
        }

        private void ZoomBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ImgView.Source == null) return;

            bool isClickedOnRoi = false;
            if (RoiRect.Visibility == Visibility.Visible && _currentRoiRect.Width > 0 && _currentRoiRect.Height > 0)
            {
                Point mousePt = e.GetPosition(ImgView);
                if (_currentRoiRect.Contains(mousePt)) isClickedOnRoi = true;
            }

            if (isClickedOnRoi) return;
            else
            {
                ContextMenu? menu = this.FindResource("DrawingContextMenu") as ContextMenu;
                if (menu != null)
                {
                    menu.PlacementTarget = sender as UIElement;
                    menu.IsOpen = true;
                }
                e.Handled = true;
            }
        }

        private void ZoomBorder_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(ImgView.Source == null) return;

            // 현재 마우스 위치를 기준으로 확대/축소하기 위해 좌표를 구하는데, 기준은 ZoomBorder.
            Point p = e.GetPosition(ZoomBorder);

            // 휠을 올리면 (Delta > 0) 1.2 배 확대, 내리면 1.2배 축소
            double zoom = e.Delta > 0 ? 1.2 : (1.0 / 1.2);

            // 배율 적용.
            imgScale.ScaleX *= zoom;
            imgScale.ScaleY *= zoom;

            // 위치 보정 - 마우스 포인터 위치(p)가 줌 동작 이후에도 같은 곳에 있도록 이동.
            // 거의 표준 공식.
            imgTranslate.X = p.X -(p.X - imgTranslate.X) * zoom;
            imgTranslate.Y = p.Y -(p.Y - imgTranslate.Y) * zoom;
        }

        private void ZoomBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 휠버튼이 클릭되었고, 이미지가 로드된 상태라면,
            if (e.ChangedButton == MouseButton.Middle && ImgView.Source != null)
            {
                var border = sender as Border;
                border.CaptureMouse();
                _start = e.GetPosition(border);
                _origin = new Point(imgTranslate.X, imgTranslate.Y);
                _isDragging = true;
                Cursor = Cursors.SizeAll;
            }
            else if (e.ChangedButton == MouseButton.Left && ImgView.Source != null) // ROI 또는 기타 도형 그리기 시작
            {
                Point mousePos = e.GetPosition(ImgView);
                var bitmap  = ImgView.Source as BitmapSource;

                if (_currentDrawMode == DrawingMode.Roi)
                {
                    if (mousePos.X >= 0 && mousePos.X < bitmap.PixelWidth && mousePos.Y >= 0 && mousePos.Y < bitmap.PixelHeight)
                    {
                        _isRoiDrawing = true;
                        _roiStartPoint = mousePos;

                        RoiRect.Visibility = Visibility.Visible;
                        RoiRect.Width = 0;
                        RoiRect.Height = 0;
                        UpdateRoiVisual(mousePos, mousePos);

                        ImgCanvas.CaptureMouse();
                    }
                }
            }
        }

        private void UpdateRoiVisual(Point start, Point end)
        {
            // 두 점 중 작은 값이 왼쪽/위쪽(X, Y), 차이값이 너비/높이(W, H)
            double x = Math.Min(start.X, end.X);
            double y = Math.Min(start.Y, end.Y);
            double w = Math.Abs(end.X - start.X);
            double h = Math.Abs(end.Y - start.Y);

            // 1. 논리적 ROI 데이터 저장 (이미지 기준 픽셀 - 나중에 자를 때 씀)
            _currentRoiRect = new Rect(x, y, w, h);

            // 2. 화면 표시용 좌표 변환 (Zoom/Pan 적용)
            // 이미지가 확대되어 있으면 사각형도 확대된 위치에 그려야 함
            // 공식: 이미지좌표 * 배율 + 이동거리
            double screenX = x * imgScale.ScaleX + imgTranslate.X;
            double screenY = y * imgScale.ScaleY + imgTranslate.Y;
            double screenW = w * imgScale.ScaleX;
            double screenH = h * imgScale.ScaleY;

            // 이미 위에서 screenX, screenY, screenW, screenH 계산했기 때문에 자동 확대/축소 기능을 꺼버림.
            RoiRect.RenderTransform = null; // 중요: 기존 이미지의 Transform을 따라가지 않도록 해제 : 충돌 방지용 초기화

            RoiRect.Width = screenW;        // 계산된 화면 크기 적용
            RoiRect.Height = screenH;
            Canvas.SetLeft(RoiRect, screenX);   // 부모 캔버스(ImgCanvas) 위에서 RoiRect의 X 위치는 screenX라고 지정.
            Canvas.SetTop(RoiRect, screenY);    // 부모 캔버스(ImgCanvas) 위에서 RoiRect의 Y 위치는 screenY라고 지정.
        }

        private void ZoomBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && e.ChangedButton == MouseButton.Middle)
            {
                var border = sender as Border;
                border.ReleaseMouseCapture();
                _isDragging = false;
                Cursor = Cursors.Arrow;
            }
            else if(_isRoiDrawing && e.ChangedButton == MouseButton.Left)
            {
                _isRoiDrawing = false;
                ImgCanvas.ReleaseMouseCapture();

                //double x = Math.Min(_roiStartPoint.X, RoiRect.Tag is Point p ? p.X : 0);
            }
        }

        private void ZoomBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)         // 이미지 이동중 이라면,
            {
                var border = sender as Border;
                Point v = e.GetPosition(border);    // 현재 마우스 위치.

                // 이동 거리 = 현재위치(v) - 시작위치(_start)
                // 새 이미지 위치 = 원래위치(_origin) + 이동거리
                imgTranslate.X = _origin.X + (v.X - _start.X);
                imgTranslate.Y = _origin.Y + (v.Y - _start.Y);
            }
            else if(_isRoiDrawing)
            {
                Point currentPos = e.GetPosition(ImgView);
                var bitmap = ImgView.Source as BitmapSource;

                // 이미지 범위 제한 (Clamp)
                if (currentPos.X < 0) currentPos.X = 0;
                if (currentPos.Y < 0) currentPos.Y = 0;
                if (currentPos.X > bitmap.PixelWidth) currentPos.X = bitmap.PixelWidth;
                if (currentPos.Y > bitmap.PixelHeight) currentPos.Y = bitmap.PixelHeight;

                UpdateRoiVisual(_roiStartPoint, currentPos);
            }

                var vm = this.DataContext as MainViewModel;
            if (vm != null)
            {
                if(ImgView.Source is BitmapSource bitmap)
                {
                    Point p = e.GetPosition(ImgView);   // 이미지 컨트롤 기준의 좌표를 가져옴.

                    int currentX = (int)p.X;
                    int currentY = (int)p.Y;

                    if (currentX >= 0 && currentX < bitmap.PixelWidth &&
                        currentY >= 0 && currentY < bitmap.PixelHeight)
                    {
                        // 좌표 출력
                        vm.MouseCoordinationInfo = $"(X: {currentX}, Y: {currentY})";
                    }
                    else
                    {
                        // 좌표 출력
                        vm.MouseCoordinationInfo = "(X: 0, Y: 0)";
                    }
                }
            }
        }

        private void ZoomBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ImgView.Source != null) FitImageToScreen();
        }
        private void ImgView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => FitImageToScreen(), DispatcherPriority.ContextIdle);
        }

        public void FitImageToScreen()
        {
            ZoomBorder.UpdateLayout();
            ImgView.UpdateLayout();

            if (ImgView.Source == null || ZoomBorder.ActualWidth == 0 || ZoomBorder.ActualHeight == 0)
            {
                return;
            }

            var imageSource = ImgView.Source as BitmapSource;
            if (imageSource == null || imageSource.Width == 0 || imageSource.Height == 0)
                return;

            // 배율 1.0
            imgScale.ScaleX = 1.0;
            imgScale.ScaleY = 1.0;
            // 이동 없음, 원점
            imgTranslate.X = 0;
            imgTranslate.Y = 0;

            double scaleX = ZoomBorder.ActualWidth / imageSource.Width;     // 화면 너비 대비 이미지 너비의 비율
            double scaleY = ZoomBorder.ActualHeight / imageSource.Height;   // 화면 높이 대비 이미지 높이의 비율

            double scale = Math.Min(scaleX, scaleY);    // 최종 배율 결정, 가로 세로 비율 중 더 작은 값을 선택.

            if (scale > 1.0) scale = 1.0;       // 확대 금지.

            // 약간의 여백 주기.
            imgScale.ScaleX = scale * 0.95;
            imgScale.ScaleY = scale * 0.95;

            // 중앙 정렬 위치 계산.
            double finalWidth = imageSource.Width * imgScale.ScaleX;
            double finalHeight = imageSource.Height * imgScale.ScaleY;

            // (화면 너비 - 이미지 너비) / 2 공식을 사용하여
            // 남은 여백의 절반만큼 이동시키면 정중앙에 위치
            imgTranslate.X = (ZoomBorder.ActualWidth - finalWidth) / 2;
            imgTranslate.Y = (ZoomBorder.ActualHeight - finalHeight) / 2;
        }

        private void MenuItem_Crop_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRoiRect.Width <= 0 || _currentRoiRect.Height <= 0) return;

            var vm = this.DataContext as MainViewModel;
            if (vm != null)
            {
                vm.CropImage((int)_currentRoiRect.X, (int)_currentRoiRect.Y, (int)_currentRoiRect.Width, (int)_currentRoiRect.Height);

                RoiRect.Visibility = Visibility.Collapsed;

                FitImageToScreen();
            }
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        

        private void Menu_DrawRoi_Click(object sender, RoutedEventArgs e)
        {
            _currentDrawMode = DrawingMode.Roi;
            Cursor = Cursors.Cross;
        }

        private void Menu_Clear_Click(object sender, RoutedEventArgs e)
        {
            // 그려진 모든 도형 삭제
            OverlayCanvas.Children.Clear();
            _currentDrawMode = DrawingMode.None;
            Cursor = Cursors.Arrow;
        }

        private void Menu_Fit_Click(object sender, RoutedEventArgs e)
        {
            FitImageToScreen();
        }
    }
}