using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Vision_OpenCV_App
{
    /// <summary>
    /// HistogramWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HistogramWindow : Window
    {
        private float[] _data;
        private int _channel;

        public HistogramWindow(float[] data, int channel)
        {
            InitializeComponent();
            _data = data;
            _channel = channel;

            DrawGraph();
        }

        private void DrawGraph()
        {
            if (_data == null || _data.Length == 0) return;
            if (GraphCanvas.ActualWidth == 0 || GraphCanvas.ActualHeight == 0) return;

            GraphCanvas.Children.Clear();

            // 여백 설정 (축과 라벨 공간 확보)
            double margin = 40;
            double w = GraphCanvas.ActualWidth - margin * 2;
            double h = GraphCanvas.ActualHeight - margin * 2;

            double maxVal = _data.Max();
            if (maxVal == 0) maxVal = 1;

            // X축 간격
            double step = w / _data.Length;

            // 색상 결정
            Brush brush = Brushes.Gray;
            string chName = "Gray";
            if (_channel == 0)
            {
                brush = Brushes.Blue;
                chName = "Blue/Gray";
            }
            else if (_channel == 1)
            {
                brush = Brushes.Green;
                chName = "Green";
            }
            else if (_channel == 2)
            {
                brush = Brushes.Red;
                chName = "Red";
            }

            TxtInfo.Text = $"Channel: {chName} | Bins: {_data.Length} | Max Count: {maxVal:F0}";

            // Y축 그리기
            Line yAxis = new Line 
            {
                X1 = margin, Y1 = margin,
                X2 = margin, Y2 = margin + h,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(yAxis);

            // X축 그리기
            Line xAxis = new Line
            {
                X1 = margin, Y1 = margin + h,
                X2 = margin + w, Y2 = margin + h,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(xAxis);

            // Draw poligon-Line
            Polyline polyline = new Polyline
            {
                Stroke = brush,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, ((SolidColorBrush)brush).Color.R, 
                ((SolidColorBrush)brush).Color.G, 
                ((SolidColorBrush)brush).Color.B))
            };

            // 데이터 포인트 추가
            // (0, 0)은 화면 좌상단이므로, Y값은 [margin + h]에서 빼줘야 위로 올라가는 그래프가 됨.
            // 시작점 (0,0) 추가 (채우기 효과를 위해)
            polyline.Points.Add(new Point(margin, margin + h));

            // Point 추가: (0, 높이) 부터 시작해서 닫힌 도형을 만들려면 Path를 써야 하지만, 간단히 선만 그림.
            for (int i = 0; i<_data.Length; i++)
            {
                double x = margin + (i * step);

                // Y 좌표는 위에서 아래로 증가하므로, h - 값으로 뒤집히도록 계산 필요.
                //double y = h - (_data[i] /maxVal * h);

                // 높이 계산: (현재 값 / 최대값) * 그래프 높이
                double y = (margin + h) - (_data[i] / maxVal * h);
                polyline.Points.Add(new Point(x, y));
            }

            // 끝점 (마지막X, 0) 추가 (채우기 효과를 위해)
            polyline.Points.Add(new Point(margin + w, margin + h));

            GraphCanvas.Children.Add(polyline);

            // 라벨 및 눈금 그리기
            // Y축 라벨 (최대 값)
            TextBlock maxLabel = new TextBlock
            {
                Text = maxVal.ToString("F0"),
                FontSize = 10,
                Foreground = Brushes.Black
            };
            // 텍스트 위치 잡기 
            Canvas.SetLeft(maxLabel, 5);
            Canvas.SetTop(maxLabel, margin - 5);
            GraphCanvas.Children.Add(maxLabel);

            // Y축 라벨 (0)
            TextBlock zeroYLabel = new TextBlock
            {
                Text = "0",
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(zeroYLabel, 25);
            Canvas.SetTop(zeroYLabel, margin + h - 5);
            GraphCanvas.Children.Add(zeroYLabel);

            // Y축 이름
            TextBlock yAxisTitle = new TextBlock
            {
                Text = "Count",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                RenderTransform = new RotateTransform(-90) // 세로로 회전
            };
            Canvas.SetLeft(yAxisTitle, 10);
            Canvas.SetTop(yAxisTitle, margin + h / 2 + 15);
            GraphCanvas.Children.Add(yAxisTitle);

            // X축 라벨 (시작 0)
            TextBlock startLabel = new TextBlock
            {
                Text = "0",
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(startLabel, margin);
            Canvas.SetTop(startLabel, margin + h + 5);
            GraphCanvas.Children.Add(startLabel);

            // X축 라벨 (중간 128)
            TextBlock midLabel = new TextBlock
            {
                Text = "128",
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(midLabel, margin + w / 2 - 10);
            Canvas.SetTop(midLabel, margin + h + 5);
            GraphCanvas.Children.Add(midLabel);

            // X축 라벨 (끝 255)
            TextBlock endLabel = new TextBlock
            {
                Text = "255",
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(endLabel, margin + w - 15);
            Canvas.SetTop(endLabel, margin + h + 5);
            GraphCanvas.Children.Add(endLabel);

            // X축 이름
            TextBlock xAxisTitle = new TextBlock
            {
                Text = "Intensity Value",
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(xAxisTitle, margin + w / 2 - 40);
            Canvas.SetTop(xAxisTitle, margin + h + 20);
            GraphCanvas.Children.Add(xAxisTitle);

        }

        private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGraph();
        }
    }
}
