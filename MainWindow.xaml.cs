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

    public enum ResizeDirection
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,    // 상
        Bottom, // 하
        Left,   // 좌
        Right   // 우
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

        // ROI Resize Handle 관련 변수
        private bool _isResizing = false;
        private ResizeDirection _resizeDirection = ResizeDirection.None;

        // ROI 사각형 이동 상태관련 변수
        private bool _isMovingRoi = false;
        // WPF에서 방향과 크기를 가진 물리량을 표현하기위해 사용하는 구조체.
        // ROI 사각형을 마우스로 드래그해서 이동시킬 때, "마우스가 움직인 만큼 사각형도 똑같이 움직여야" 합니다.
        private Vector _moveOffset;         // 클릭한 지점과 사각형 좌상단 사이의 거리(오차)를 저장.

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

                if (RoiRect.Visibility == Visibility.Visible && _currentRoiRect.Contains(mousePos))
                {
                    _isMovingRoi = true;
                    _moveOffset = mousePos - new Point(_currentRoiRect.X, _currentRoiRect.Y);

                    ImgCanvas.CaptureMouse();
                    return;
                }
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
                else if (_currentDrawMode != DrawingMode.Roi)
                {
                    _drawStartPoint = mousePos; // 현재 마우스 좌표.

                    if (_currentDrawMode == DrawingMode.Line)
                    {
                        _tempShape = new Line()
                        {
                            Stroke = Brushes.Yellow,
                            StrokeThickness = 5,
                            X1 = _drawStartPoint.X,
                            Y1 = _drawStartPoint.Y,
                            X2 = _drawStartPoint.X,
                            Y2 = _drawStartPoint.Y
                        };
                    }
                    else if (_currentDrawMode == DrawingMode.Circle)
                    {
                        _tempShape = new Ellipse()
                        {
                            Stroke = Brushes.Lime,
                            StrokeThickness = 5,
                            Width = 0,
                            Height = 0
                        };

                        Canvas.SetLeft(_tempShape, _drawStartPoint.X);
                        Canvas.SetTop(_tempShape, _drawStartPoint.Y);
                    }
                    else if (_currentDrawMode == DrawingMode.Rectangle)
                    {
                        _tempShape = new Rectangle()
                        {
                            Stroke = Brushes.Cyan,
                            StrokeThickness = 5,
                            Width = 0,
                            Height = 0
                        };
                        Canvas.SetLeft(_tempShape, _drawStartPoint.X);
                        Canvas.SetTop(_tempShape, _drawStartPoint.Y);
                    }

                    if(_tempShape != null)
                    {
                        // _tempShape 가 어떤 도형의 객체로 할당되었다면, 할당된 도형을 OverlayCavas 위에 추가(Add) 시켜라.
                        OverlayCanvas.Children.Add(_tempShape);
                        // 그리는 동안 마우스를 권한을 가지고 있겠다.
                        ZoomBorder.CaptureMouse();
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

            // Resize Handle 위치 업데이트
            UpdateResizeHandle(Handle_TL, screenX, screenY);
            UpdateResizeHandle(Handle_TR, screenX + screenW, screenY);
            UpdateResizeHandle(Handle_BL, screenX, screenY + screenH);
            UpdateResizeHandle(Handle_BR, screenX + screenW, screenY + screenH);

            // 상하좌우 핸들은 각 변의 중앙에 위치
            UpdateResizeHandle(Handle_Top, screenX + screenW / 2, screenY);
            UpdateResizeHandle(Handle_Bottom, screenX + screenW / 2, screenY + screenH);
            UpdateResizeHandle(Handle_Left, screenX, screenY + screenH / 2);
            UpdateResizeHandle(Handle_Right, screenX + screenW, screenY + screenH / 2);

        }

        private void UpdateResizeHandle(Rectangle handle, double x, double y)
        {
            handle.Visibility = Visibility.Visible;
            Canvas.SetLeft(handle, x - 5);
            Canvas.SetTop(handle, y - 5);
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
            else if(_isResizing)
            {
                _isResizing = false;
                _resizeDirection = ResizeDirection.None;
                ImgCanvas.ReleaseMouseCapture();
            }
            else if(_isMovingRoi)
            {
                _isMovingRoi = false;
                ImgCanvas.ReleaseMouseCapture();
            }
            else if(_currentDrawMode != DrawingMode.None && _tempShape != null)
            {
                ZoomBorder.ReleaseMouseCapture();

                if(_currentDrawMode == DrawingMode.Line && _tempShape is Line line)
                {
                    double dist = Math.Sqrt(Math.Pow(line.X2 - line.X1, 2) + Math.Pow(line.Y2 - line.Y1, 2));

                    TextBlock tb = new TextBlock()
                    {
                        Text = $"{dist:F1}px",
                        Foreground = Brushes.Yellow,
                        Background = new SolidColorBrush(Color.FromArgb(128,0,0,0)),
                        Padding = new Thickness(2),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold
                    };

                    Canvas.SetLeft(tb, line.X2);
                    Canvas.SetTop(tb, line.Y2);

                    OverlayCanvas.Children.Add(tb);
                }


                _currentDrawMode = DrawingMode.None;
                _tempShape = null;
                Cursor = Cursors.Arrow;
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
            else if (_isRoiDrawing)
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
            else if (_isResizing && ImgView.Source != null)
            {
                var bitmap = ImgView.Source as BitmapSource;
                Point currentPos = e.GetPosition(ImgView);

                // 이미지 범위 제한
                if (currentPos.X < 0) currentPos.X = 0;
                if (currentPos.Y < 0) currentPos.Y = 0;
                if (currentPos.X > bitmap.PixelWidth) currentPos.X = bitmap.PixelWidth;
                if (currentPos.Y > bitmap.PixelHeight) currentPos.Y = bitmap.PixelHeight;

                double newX = _currentRoiRect.X;
                double newY = _currentRoiRect.Y;
                double newW = _currentRoiRect.Width;
                double newH = _currentRoiRect.Height;

                // 방향에 따른 좌표 계산
                switch (_resizeDirection)
                {
                    case ResizeDirection.TopLeft:
                        double right = _currentRoiRect.Right;
                        double bottom = _currentRoiRect.Bottom;

                        newX = Math.Min(currentPos.X, right - 1);
                        newY = Math.Min(currentPos.Y, bottom - 1);
                        newW = right - newX;
                        newH = bottom - newY;
                        break;

                    case ResizeDirection.TopRight:
                        double left = _currentRoiRect.Left;
                        bottom = _currentRoiRect.Bottom;
                        newY = Math.Min(currentPos.Y, bottom - 1);
                        newW = Math.Max(currentPos.X - left, 1);
                        newH = bottom - newY;
                        break;

                    case ResizeDirection.BottomLeft:
                        right = _currentRoiRect.Right;
                        double top = _currentRoiRect.Top;
                        newX = Math.Min(currentPos.X, right - 1);
                        newW = right - newX;
                        newH = Math.Max(currentPos.Y - top, 1);
                        break;

                    case ResizeDirection.BottomRight:
                        left = _currentRoiRect.Left;
                        top = _currentRoiRect.Top;
                        newW = Math.Max(currentPos.X - left, 1);
                        newH = Math.Max(currentPos.Y - top, 1);
                        break;

                    // [추가] 상하좌우 핸들 로직
                    case ResizeDirection.Top:
                        // X, Width 고정 / Y, Height 변경
                        bottom = _currentRoiRect.Bottom;
                        newY = Math.Min(currentPos.Y, bottom - 1);
                        newH = bottom - newY;
                        break;

                    case ResizeDirection.Bottom:
                        // X, Width 고정 / Height 변경
                        top = _currentRoiRect.Top;
                        newH = Math.Max(currentPos.Y - top, 1);
                        break;

                    case ResizeDirection.Left:
                        // Y, Height 고정 / X, Width 변경
                        right = _currentRoiRect.Right;
                        newX = Math.Min(currentPos.X, right - 1);
                        newW = right - newX;
                        break;

                    case ResizeDirection.Right:
                        // Y, Height 고정 / Width 변경
                        left = _currentRoiRect.Left;
                        newW = Math.Max(currentPos.X - left, 1);
                        break;
                }

                UpdateRoiVisual(new Point(newX, newY), new Point(newX + newW, newY + newH));

            }
            else if (_isMovingRoi && ImgView.Source != null)
            {
                var bitmap = ImgView.Source as BitmapSource;
                Point currentPos = e.GetPosition(ImgView);

                // _moveOffset 는 ZoomBorder_MouseDown 메서드에서 저장됨.
                double newX = currentPos.X - _moveOffset.X;
                double newY = currentPos.Y - _moveOffset.Y;
                double w = _currentRoiRect.Width;
                double h = _currentRoiRect.Height;

                // 이미지 영역 밖으로 나가지 않도록 제한
                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;
                if (newX + w > bitmap.PixelWidth) newX = bitmap.PixelWidth - w;
                if (newY + h > bitmap.PixelHeight) newY = bitmap.PixelHeight - h;

                // 이동된 위치로 업데이트
                UpdateRoiVisual(new Point(newX, newY), new Point(newX + w, newY + h));
            }
            else if (_currentDrawMode != DrawingMode.None && _tempShape != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(ImgView);

                if(_currentDrawMode == DrawingMode.Line)
                {
                    var line = _tempShape as Line;
                    line.X2 = currentPos.X;
                    line.Y2 = currentPos.Y;
                }
                else if(_currentDrawMode == DrawingMode.Circle || _currentDrawMode == DrawingMode.Rectangle)
                {
                    // 시작점과 현재점으로 Top-Left와 Width/Height 계산.
                    double x = Math.Min(_drawStartPoint.X, currentPos.X);
                    double y = Math.Min(_drawStartPoint.Y, currentPos.Y);
                    double w = Math.Abs(currentPos.X - _drawStartPoint.X);
                    double h = Math.Abs(currentPos.Y - _drawStartPoint.Y);

                    Canvas.SetLeft(_tempShape, x);
                    Canvas.SetTop(_tempShape, y);
                    _tempShape.Width = w;
                    _tempShape.Height = h;
                }
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

                //RoiRect.Visibility = Visibility.Collapsed;

                HideRoiAndHandles();
                FitImageToScreen();
            }
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            if(_currentRoiRect.Width <= 0 || _currentRoiRect.Height <= 0) return;

            var vm = this.DataContext as MainViewModel;
            if(vm != null)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp|TIFF Image|*.tiff|All Files|*.*";
                dlg.FileName = "ROI_Image";

                if(dlg.ShowDialog() == true)
                {
                    vm.SaveRoiImage(dlg.FileName, (int)_currentRoiRect.X, (int)_currentRoiRect.Y, 
                        (int)_currentRoiRect.Width, (int)_currentRoiRect.Height);

                    HideRoiAndHandles();

                }
            }
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
            HideRoiAndHandles();
            _currentRoiRect = new Rect(0,0,0,0);
            _currentDrawMode = DrawingMode.None;
            Cursor = Cursors.Arrow;
        }

        private void Menu_Fit_Click(object sender, RoutedEventArgs e)
        {
            FitImageToScreen();
        }

        private void HideRoiAndHandles()
        {
            if (RoiRect != null)
            {
                RoiRect.Visibility = Visibility.Collapsed;
                RoiRect.Width = 0;
                RoiRect.Height = 0;
            }


            if (Handle_TL != null) Handle_TL.Visibility = Visibility.Collapsed;
            if (Handle_TR != null) Handle_TR.Visibility = Visibility.Collapsed;
            if (Handle_BL != null) Handle_BL.Visibility = Visibility.Collapsed;
            if (Handle_BR != null) Handle_BR.Visibility = Visibility.Collapsed;
            if (Handle_Top != null) Handle_Top.Visibility = Visibility.Collapsed;
            if (Handle_Bottom != null) Handle_Bottom.Visibility = Visibility.Collapsed;
            if (Handle_Left != null) Handle_Left.Visibility = Visibility.Collapsed;
            if (Handle_Right != null) Handle_Right.Visibility = Visibility.Collapsed;

            _currentDrawMode = DrawingMode.None;
            Cursor = Cursors.Arrow;
        }

        private void ResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            if(rect == null) return;

            _isResizing = true;

            if(rect == Handle_TL) _resizeDirection = ResizeDirection.TopLeft;
            else if(rect == Handle_TR) _resizeDirection = ResizeDirection.TopRight;
            else if(rect == Handle_BL) _resizeDirection = ResizeDirection.BottomLeft;
            else if(rect == Handle_BR) _resizeDirection = ResizeDirection.BottomRight;
            else if(rect == Handle_Top) _resizeDirection = ResizeDirection.Top;
            else if(rect == Handle_Bottom) _resizeDirection = ResizeDirection.Bottom;
            else if(rect == Handle_Left) _resizeDirection = ResizeDirection.Left;
            else if(rect == Handle_Right) _resizeDirection = ResizeDirection.Right;
            else _resizeDirection = ResizeDirection.None;

            ImgCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void Menu_DrawLine_Click(object sender, RoutedEventArgs e)
        {
            _currentDrawMode = DrawingMode.Line;
            Cursor = Cursors.Cross;
        }

        private void Menu_DrawCircle_Click(object sender, RoutedEventArgs e)
        {
            _currentDrawMode = DrawingMode.Circle;
            Cursor = Cursors.Cross;
        }

        private void Menu_DrawRect_Click(object sender, RoutedEventArgs e)
        {
            _currentDrawMode = DrawingMode.Rectangle;
            Cursor = Cursors.Cross;
        }
    }
}