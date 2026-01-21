using OpenCvSharp;      // 추가.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Vision_OpenCV_App
{
    public abstract class AlgorithmParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ThresholdParams : AlgorithmParameters
    {
        private byte _thresholdValue = 128;
        public byte ThresholdValue
        {
            get => _thresholdValue;
            set
            {
                if (_thresholdValue == value) return;
                _thresholdValue = value;

                //OnPropertyChanged("ThresholdValue");
                OnPropertyChanged();
            }
        }

        private byte _thresholdMax = 255;
        public byte ThresholdMax
        {
            get => _thresholdMax;
            set
            {
                if (_thresholdMax == value) return;
                _thresholdMax = value;

                OnPropertyChanged();
            }
        }
    }


    public class OtsuParams : AlgorithmParameters
    {
        private ThresholdTypes _selectedType = ThresholdTypes.Binary;

        public ThresholdTypes SelectedType
        {
            get => _selectedType;
            set
            {
                if (_selectedType == value) return;

                _selectedType = value;
                OnPropertyChanged();
            }
        }

        // 콤보박스에 표시할 목록 (Otsu, Triangle 등 자동 계산 플래그는 제외)
        public List<ThresholdTypes> ThresholdTypesSource { get; } = new List<ThresholdTypes>
        {
            ThresholdTypes.Binary,
            ThresholdTypes.BinaryInv,
            ThresholdTypes.Trunc,
            //ThresholdTypes.ToZero,
            //ThresholdTypes.ToZeroInv
        };
    }

    public class AdaptiveThresholdParams : AlgorithmParameters
    {
        private int _blockSize = 11;
        public int BlockSize
        {
            get => _blockSize;
            set
            {
                if (_blockSize == value) return;

                if (value % 2 == 0) value++;    // 짝수가 들어오면 +1 해서 홀수 생성.
                if (value < 3) value = 3;        // 최소값 3

                _blockSize = value;
                OnPropertyChanged();
            }
        }
        private double _constantC = 2.0;
        public double ConstantC
        {
            get => _constantC;
            set
            {
                if (_constantC == value) return;
                _constantC = value;
                OnPropertyChanged();
            }
        }

        // 적응형 방식 선택(MeanC vs GaussianC)
        private AdaptiveThresholdTypes _adaptiveMethod = AdaptiveThresholdTypes.MeanC;
        public AdaptiveThresholdTypes AdaptiveMethod
        {
            get => _adaptiveMethod;
            set
            {
                if (_adaptiveMethod == value) return;
                _adaptiveMethod = value;
                OnPropertyChanged();
            }
        }

        // [추가] 결과 타입 선택 (Binary vs BinaryInv)
        private ThresholdTypes _thresholdType = ThresholdTypes.Binary;
        public ThresholdTypes ThresholdType
        {
            get => _thresholdType;
            set
            {
                if (_thresholdType == value) return;
                _thresholdType = value;
                OnPropertyChanged();
            }
        }

        public List<AdaptiveThresholdTypes> AdaptiveMethodSource { get; } = new List<AdaptiveThresholdTypes>
        {
            AdaptiveThresholdTypes.MeanC,
            AdaptiveThresholdTypes.GaussianC
        };

        public List<ThresholdTypes> ThresholdTypesSource { get; } = new List<ThresholdTypes>
        {
            ThresholdTypes.Binary,
            ThresholdTypes.BinaryInv
        };
    }


    public enum HistogramMaskMode
    {
        None,
        CenterCircle,   // 중앙 원형 마스크
        LeftHalf,       // 왼쪽 절반
        RightHalf       // 오른쪽 절반
    }

    public enum ColorChannel
    {
        Blue_Gray = 0,
        Green = 1,
        Red = 2
    }

    public class HistogramParams : AlgorithmParameters
    {
        private ColorChannel _channel = ColorChannel.Blue_Gray;
        public ColorChannel Channel
        {
            get => _channel;
            set { if (_channel != value) { _channel = value; OnPropertyChanged(); } }
        }

        private int _histSize = 256;
        public int HistSize
        {
            get => _histSize;
            set
            {
                if (value < 1) value = 1;
                if (value > 256) value = 256;
                if (_histSize != value) { _histSize = value; OnPropertyChanged(); }
            }
        }

        private float _rangeMin = 0;
        public float RangeMin
        {
            get => _rangeMin;
            set { if (_rangeMin != value) { _rangeMin = value; OnPropertyChanged(); } }
        }

        private float _rangeMax = 256;
        public float RangeMax
        {
            get => _rangeMax;
            set { if (_rangeMax != value) { _rangeMax = value; OnPropertyChanged(); } }
        }

        private HistogramMaskMode _maskMode = HistogramMaskMode.None;
        public HistogramMaskMode MaskMode
        {
            get => _maskMode;
            set { if (_maskMode != value) { _maskMode = value; OnPropertyChanged(); } }
        }

        public List<ColorChannel> ChannelSource { get; } = Enum.GetValues(typeof(ColorChannel)).Cast<ColorChannel>().ToList();
        public List<HistogramMaskMode> MaskModeSource { get; } = Enum.GetValues(typeof(HistogramMaskMode)).Cast<HistogramMaskMode>().ToList();
    }


    public class NormalizeParams : AlgorithmParameters
    {
        private double _alpha = 0.0;
        public double Alpha
        {
            get => _alpha;
            set
            {
                if (_alpha == value) return;

                _alpha = value;
                OnPropertyChanged();
            }
        }

        private double _beta = 255;
        public double Beta
        {
            get => _beta;
            set
            {
                if (_beta == value) return;
                _beta = value;
                OnPropertyChanged();
            }
        }

        private NormTypes _normType = NormTypes.MinMax;
        public NormTypes NormType
        {
            get => _normType;
            set
            {
                if (_normType == value) return;
                _normType = value;
                OnPropertyChanged();
            }
        }

        public List<NormTypes> NormTypeSource { get; } = new List<NormTypes>
        {
            NormTypes.MinMax,
            NormTypes.L1,
            NormTypes.L2,
            NormTypes.INF
        };
    }



    public class EqualizeParams : AlgorithmParameters
    {
        // EqualizeHist는 별도 파라미터가 없습니다.
    }

    // [신규] CLAHE 파라미터 추가
    public class ClaheParams : AlgorithmParameters
    {
        // 대비 제한 값 (기본 40.0)
        private double _clipLimit = 40.0;
        public double ClipLimit
        {
            get => _clipLimit;
            set { if (_clipLimit != value) { _clipLimit = value; OnPropertyChanged(); } }
        }

        // 타일 그리드 크기 (기본 8 -> 8x8)
        private int _tileGridSize = 8;
        public int TileGridSize
        {
            get => _tileGridSize;
            set
            {
                if (value < 1) value = 1;
                if (_tileGridSize != value) { _tileGridSize = value; OnPropertyChanged(); }
            }
        }
    }


    // [신규] Geometric Transformation 파라미터 (통합)
    public class GeometricParams : AlgorithmParameters
    {
        // 이동 (X축)
        private double _moveX = 0;
        public double MoveX
        {
            get => _moveX;
            set
            {
                if (_moveX == value) return;

                _moveX = value;
                OnPropertyChanged();
            }
        }

        // 이동 (Y축)
        private double _moveY = 0;
        public double MoveY
        {
            get => _moveY;
            set
            {
                if (_moveY == value) return;

                _moveY = value;
                OnPropertyChanged();
            }
        }

        // 회전 각도 (Degrees)
        private double _angle = 0;
        public double Angle
        {
            get => _angle;
            set
            {
                if (_angle == value) return;

                _angle = value;
                OnPropertyChanged();
            }
        }

        // 확대/축소 비율 (Scale Factor, 1.0 = 원본)
        private double _scale = 1.0;
        public double Scale
        {
            get => _scale;
            set
            {
                if (_scale == value) return;

                _scale = value;
                OnPropertyChanged();
            }
        }

        // 보간법 (Interpolation Method)
        private InterpolationFlags _interpolation = InterpolationFlags.Linear;
        public InterpolationFlags Interpolation
        {
            get => _interpolation;
            set
            {
                if (_interpolation != value) return;

                _interpolation = value;
                OnPropertyChanged();
            }
        }

        // 보간법 선택 목록
        public List<InterpolationFlags> InterpolationSource { get; } = new List<InterpolationFlags>
        {
            InterpolationFlags.Nearest, // 빠름, 계단현상
            InterpolationFlags.Linear,  // 기본, 부드러움
            InterpolationFlags.Cubic,   // 더 부드러움 (느림)
            InterpolationFlags.Lanczos4 // 고품질 (가장 느림)
        };
    }

    public class AffineParams : AlgorithmParameters
    {
        // 화면에 보여줄 텍스트 정보
        public string PointInfo => $"P1:({Pt1.X:F0}, {Pt1.Y:F0}) P2:({Pt2.X:F0}, {Pt2.Y:F0}) P3:({Pt3.X:F0}, {Pt3.Y:F0})";

        // 3개의 점 좌표 (화면 표시 및 warpAffine 입력)
        private Point2f _pt1 = new Point2f();
        public Point2f Pt1
        {
            get => _pt1;
            set
            {
                if (_pt1 == value) return;

                _pt1 = value;
                OnPropertyChanged(nameof(PointInfo));
            }
        }
        private Point2f _pt2 = new Point2f();
        public Point2f Pt2
        {
            get => _pt2;
            set
            {
                if (_pt2 == value) return;

                _pt2 = value;
                OnPropertyChanged(nameof(PointInfo));
            }
        }
        private Point2f _pt3 = new Point2f();
        public Point2f Pt3
        {
            get => _pt3;
            set
            {
                if (_pt3 == value) return;

                _pt3 = value;
                OnPropertyChanged(nameof(PointInfo));
            }
        }


        // Interpolation Method
        private InterpolationFlags _interpolation = InterpolationFlags.Linear;
        public InterpolationFlags Interpolation
        {
            get => _interpolation;
            set
            {
                if (_interpolation == value) return;

                _interpolation = value;
                OnPropertyChanged();
            }
        }

        public List<InterpolationFlags> InterpolationSource { get; } = new List<InterpolationFlags>
        {
            InterpolationFlags.Nearest,
            InterpolationFlags.Linear,
            InterpolationFlags.Cubic,
            InterpolationFlags.Lanczos4
        };
    }


    // [신규] Perspective Transform 파라미터 (4개의 점)
    public class PerspectiveParams : AlgorithmParameters
    {
        public string PointInfo => $"P1:({Pt1.X:F0},{Pt1.Y:F0})  P2:({Pt2.X:F0},{Pt2.Y:F0})\nP3:({Pt3.X:F0},{Pt3.Y:F0})  P4:({Pt4.X:F0},{Pt4.Y:F0})";

        private Point2f _pt1 = new Point2f(0, 0);
        public Point2f Pt1 
        { 
            get => _pt1; 
            set 
            {
                if (_pt1 == value) return; 

                _pt1 = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PointInfo));
            } 
        }

        private Point2f _pt2 = new Point2f(0, 0);
        public Point2f Pt2 
        { 
            get => _pt2; 
            set 
            {
                if (_pt2 == value) return; 

                _pt2 = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PointInfo));
            } 
        }

        private Point2f _pt3 = new Point2f(0, 0);
        public Point2f Pt3 
        { 
            get => _pt3; 
            set 
            {
                if (_pt3 == value) return; 

                _pt3 = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PointInfo)); 
            } 
        }

        private Point2f _pt4 = new Point2f(0, 0);
        public Point2f Pt4 
        { 
            get => _pt4; 
            set 
            {
                if (_pt4 == value) return; 

                _pt4 = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PointInfo));
            } 
        }

        

        private InterpolationFlags _interpolation = InterpolationFlags.Linear;
        public InterpolationFlags Interpolation
        {
            get => _interpolation;
            set { if (_interpolation != value) { _interpolation = value; OnPropertyChanged(); } }
        }

        public List<InterpolationFlags> InterpolationSource { get; } = new List<InterpolationFlags>
        {
            InterpolationFlags.Nearest,
            InterpolationFlags.Linear,
            InterpolationFlags.Cubic,
            InterpolationFlags.Lanczos4
        };
    }

    // 왜곡 모드 선택 열거형
    public enum DistortionMode
    {
        SinCosWave,     // 기존 물결 효과
        LensSimulate    // 신규 볼록/오목 렌즈 효과
    }

    // [신규] Remap (Lens Distortion) 파라미터
    public class RemapParams : AlgorithmParameters
    {
        private DistortionMode _mode = DistortionMode.SinCosWave;
        public DistortionMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;

                _mode = value;
                OnPropertyChanged();
            }
        }

        // 파장 (Wavelength)
        private double _wavelength = 20.0;
        public double Wavelength
        {
            get => _wavelength;
            set 
            {
                if (_wavelength == value) return; 
                    
                _wavelength = value; 
                OnPropertyChanged();
            }
        }

        // 진폭 (Amplitude)
        private double _amplitude = 10.0;
        public double Amplitude
        {
            get => _amplitude;
            set
            {
                if (_amplitude == value) return; 
                
                _amplitude = value; 
                OnPropertyChanged();
            }
        }

        // 위상 (Phase)
        private double _phase = 0.0;
        public double Phase
        {
            get => _phase;
            set
            {
                if (_phase == value) return; 
               
                _phase = value; 
                OnPropertyChanged();
            }
        }

        public string PointInfo => $"Center: ({Center.X:F0}, {Center.Y:F0})";

        private Point2f _center = new Point2f(0, 0);
        public Point2f Center
        {
            get => _center;
            set
            {
                if (_center == value) return;
                _center = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PointInfo));
            }
        }

        private double _exponent = 1.0; // 1.0=원본, <1.0 오목, >1.0 볼록
        public double Exponent
        {
            get => _exponent;
            set 
            { 
                if (_exponent == value) return; 
                
                _exponent = value; 
                OnPropertyChanged();
            }
        }

        private double _scale = 0.5; // 반경 크기 비율
        public double Scale
        {
            get => _scale;
            set 
            {
                if (_scale == value) return; 
                
                _scale = value; 
                OnPropertyChanged();
            }
        }



        // 보간법
        private InterpolationFlags _interpolation = InterpolationFlags.Linear;
        public InterpolationFlags Interpolation
        {
            get => _interpolation;
            set 
            {
                if (_interpolation == value) return; 
                
                _interpolation = value; 
                OnPropertyChanged();  
            }
        }

        public List<InterpolationFlags> InterpolationSource { get; } = new List<InterpolationFlags>
        {
            InterpolationFlags.Nearest,
            InterpolationFlags.Linear,
            InterpolationFlags.Cubic,
            InterpolationFlags.Lanczos4
        };

        // 소스 목록
        public List<DistortionMode> ModeSource { get; } = Enum.GetValues(typeof(DistortionMode)).Cast<DistortionMode>().ToList();
    }


    public class CameraCalibrationParams : AlgorithmParameters
    {
        // 캘리브레이션 실행 버튼과 연결될 커맨드
        //public ICommand CalibrateCommand { get; set; }

        // 계산된 RMS 오차를 화면에 보여줄 문자열
        private string _rmsText = "RMS Error: N/A";
        public string RmsText
        {
            get => _rmsText;
            set
            {
                if (_rmsText == value) return;

                _rmsText = value;
                OnPropertyChanged();
            }
        }

        // 체스보드 가로 내부 코너 개수
        private int _patternWidth = 9;
        public int PatternWidth
        {
            get => _patternWidth;
            set 
            {
                if (_patternWidth == value) return; 

                _patternWidth = value; 
                OnPropertyChanged();
            }
        }

        // 체스보드 세로 내부 코너 개수
        private int _patternHeight = 6;
        public int PatternHeight
        {
            get => _patternHeight;
            set 
            {
                if (_patternHeight == value) return; 

                _patternHeight = value; 
                OnPropertyChanged();
            }
        }
    }


    // Manual Filter parameter (Filter2D)
    public enum FilterKernelType
    {
        Blur,
        Edge,
        Sharpen,
    }

    public class ManualFilterParams : AlgorithmParameters
    {
        private FilterKernelType _selectedKernelType = FilterKernelType.Blur;
        public FilterKernelType SelectedKernelType
        {
            get => _selectedKernelType;
            set
            {
                if (_selectedKernelType == value) return;

                _selectedKernelType = value;
                UpdateKernelValue();
                OnPropertyChanged();
            }
        }

        private string _kernelInfo = "";
        public string KernelInfo
        {
            get => _kernelInfo;
            private set
            {
                if (_kernelInfo == value) return;

                _kernelInfo = value;
                OnPropertyChanged();
            }
        }

        // 실제 연산에 사용할 커널 데이터 배열
        public float[] KernelData { get; private set; }

        private string _ddepthInfo = "CV_8U";
        public string DDepthInfo
        {
            get => _ddepthInfo;
            private set
            {
                if (_ddepthInfo == value) return;

                _ddepthInfo = value;
                OnPropertyChanged();
            }
        }

        private int _anchorX = -1;
        public int AnchorX
        {
            get => _anchorX;
            set
            {
                if (_anchorX == value) return;

                _anchorX = value;
                OnPropertyChanged();
            }
        }

        private int _anchorY = -1;
        public int AnchorY
        {
            get => _anchorY;
            set
            {
                if (_anchorY == value) return;

                _anchorY = value;
                OnPropertyChanged();
            }
        }

        private double _delta = 0.0;
        public double Delta
        {
            get => _delta;
            set
            {
                if (_delta == value) return;

                _delta = value;
                OnPropertyChanged();
            }
        }

        private BorderTypes _borderType = BorderTypes.Default;
        public BorderTypes BorderType
        {
            get => _borderType;
            set
            {
                if (_borderType == value) return;

                _borderType = value;
                OnPropertyChanged();
            }
        }

        public List<FilterKernelType> KernelTypeSource { get; } = Enum.GetValues(typeof(FilterKernelType))  // FilterKernelType 안에 정의된 모든 값(Blur, Edge, Sharpen)을 가져옴 (Array 형태)
            .Cast<FilterKernelType>()       // 가져온 값들을 명확하게 FilterKernelType이라는 구체적인 자료형으로 변환
            .ToList();                      // 최종적으로 WPF가 가장 잘 다루는 컬렉션 형태인 List로 변환하여 저장
        public List<BorderTypes> BorderTypeSource { get; } = Enum.GetValues(typeof(BorderTypes)).Cast<BorderTypes>().ToList();

        public ManualFilterParams()
        {
            UpdateKernelValue();
        }



        private void UpdateKernelValue()
        {
            switch (_selectedKernelType)
            {
                case FilterKernelType.Blur:
                    // 3x3 Average Blur (Sum=1)
                    KernelData = new float[] {
                        1/9f, 1/9f, 1/9f,
                        1/9f, 1/9f, 1/9f,
                        1/9f, 1/9f, 1/9f
                    };
                    KernelInfo = "[[1/9, 1/9, 1/9]\n [1/9, 1/9, 1/9]\n [1/9, 1/9, 1/9]]";
                    DDepthInfo = "CV_8U (-1)";
                    break;

                case FilterKernelType.Edge:
                    // Strong Edge (Sum=0, 8-neighbors)
                    // 대각선 방향까지 포함하여 8방향 엣지를 검출합니다.
                    KernelData = new float[] {
                         -1, -1, -1,
                         -1,  8, -1,
                         -1, -1, -1
                    };
                    KernelInfo = "[[-1, -1, -1]\n [-1,  8, -1]\n [-1, -1, -1]]";
                    DDepthInfo = "CV_16S"; // 음수 표현 필요
                    break;

                case FilterKernelType.Sharpen:
                    // Strong Sharpening (Sum=1, 8-neighbors)
                    // 8방향의 데이터를 뺌으로써 더욱 날카로운 느낌을 줍니다.
                    KernelData = new float[] {
                        -1, -1, -1,
                        -1,  9, -1,
                        -1, -1, -1
                    };
                    KernelInfo = "[[-1, -1, -1]\n [-1,  9, -1]\n [-1, -1, -1]]";
                    DDepthInfo = "CV_8U (-1)";
                    break;
            }
        }
    }

    // Auto Filter Parameters
    public enum AutoFilterType
    {
        AverageBlur,        // Cv2.Blur
        BoxFilter,          // Cv2.BoxFilter
        GaussianBlur,       // Cv2.GaussianBlur
        MedianBlur,         // Cv2.MedianBlur
        BilateralFilter     // Cv2.BilateralFilter
    }

    public class AutoFilterParams : AlgorithmParameters
    {
        private AutoFilterType _selectedFilterType = AutoFilterType.AverageBlur;
        public AutoFilterType SelectedFilterType
        {
            get => _selectedFilterType;
            set
            {
                if (_selectedFilterType == value) return;

                _selectedFilterType = value;
                OnPropertyChanged();
            }
        }

        // Kernel Size (for Blur, BoxFilter, GaussianBlur, MedianBlur)
        private int _kernelSize = 3;
        public int KernelSize
        {
            get => _kernelSize;
            set
            {
                if(value == _kernelSize) return;

                // 커널 크기는 홀수만 허용 (3,5,7,...)
                if (value % 2 == 0) value++;
                if (value < 1) value = 1;

                _kernelSize = value;
                OnPropertyChanged();
            }
        }

        // Gaussian Blur 전용.
        private double _sigmaX = 1.0;
        public double SigmaX
        {
            get => _sigmaX;
            set
            {
                if (_sigmaX == value) return;

                _sigmaX = value;
                OnPropertyChanged();
            }
        }

        private double _sigmaY = 0.0;
        public double SigmaY
        {
            get => _sigmaY;
            set
            {
                if (_sigmaY == value) return;

                _sigmaY = value;
                OnPropertyChanged();
            }
        }

        // Bilateral Filter 전용
        private int _diameter = 9; // 필터링에 사용될 이웃 픽셀의 지름 (음수면 sigmaSpace로 계산)
        public int Diameter
        {
            get => _diameter;
            set 
            {
                if (_diameter == value) return; 

                _diameter = value; 
                OnPropertyChanged(); 
            }
        }

        private double _sigmaColor = 75.0; // 색공간 표준편차 (클수록 색 차이가 큰 픽셀도 섞임)
        public double SigmaColor
        {
            get => _sigmaColor;
            set 
            {
                if (_sigmaColor == value) return; 
                
                _sigmaColor = value; 
                OnPropertyChanged();
            }
        }

        private double _sigmaSpace = 75.0; // 좌표공간 표준편차 (클수록 멀리 있는 픽셀도 영향을 줌)
        public double SigmaSpace
        {
            get => _sigmaSpace;
            set 
            {
                if (_sigmaSpace == value) return; 

                _sigmaSpace = value; 
                OnPropertyChanged();
            }
        }

        public List<AutoFilterType> FilterTypeSource { get; } = Enum.GetValues(typeof(AutoFilterType))
            .Cast<AutoFilterType>()
            .ToList();

    }

    public enum EdgeDetectionType
    {
        BasicDifferential,  // 기본 미분
        Roberts,            // Roberts 교차 미분
        Prewitt,            // Prewitt 미분
        Sobel,              // Sobel 미분
        Scharr,             // Scharr 미분
        Laplacian,           // 라플라시안
        Canny               // 캐니 엣지 검출
    }

    public class EdgeDetectionParams : AlgorithmParameters
    {
        private EdgeDetectionType _selectedType = EdgeDetectionType.BasicDifferential;
        public EdgeDetectionType SelectedType
        {
            get => _selectedType;
            set 
            {
                if (_selectedType == value) return; 

                _selectedType = value; 
                OnPropertyChanged();
            }
        }

        // 공통: 커널 크기 (Sobel, Scharr, Laplacian, Canny용)
        private int _ksize = 3;
        public int KSize
        {
            get => _ksize;
            set
            {
                // Sobel, Laplacian 등은 홀수만 가능 (1, 3, 5, 7)
                if (value % 2 == 0) value++;
                if (value < 1) value = 1;
                if (value > 7) value = 7;

                if (_ksize == value) return; 

                _ksize = value; 
                OnPropertyChanged();
            }
        }

        // Canny 전용: Threshold1 (낮은 임계값)
        private double _cannyTh1 = 50;
        public double CannyTh1
        {
            get => _cannyTh1;
            set 
            {
                if (_cannyTh1 == value) return; 
                
                _cannyTh1 = value; 
                OnPropertyChanged();
            }
        }

        // Canny 전용: Threshold2 (높은 임계값)
        private double _cannyTh2 = 150;
        public double CannyTh2
        {
            get => _cannyTh2;
            set 
            {
                if (_cannyTh2 == value) return; 

                _cannyTh2 = value; 
                OnPropertyChanged();
            }
        }

        // Sobel/Laplacian 전용: Scale
        private double _scale = 1.0;
        public double Scale
        {
            get => _scale;
            set
            {
                if (_scale == value) return;

                _scale = value;
                OnPropertyChanged();
            }
        }

        // Sobel/Laplacian 전용: Delta
        private double _delta = 0.0;
        public double Delta
        {
            get => _delta;
            set 
            {
                if (_delta == value) return;

                _delta = value; 
                OnPropertyChanged();
            }
        }

        public List<EdgeDetectionType> EdgeTypeSource { get; } = Enum.GetValues(typeof(EdgeDetectionType))
            .Cast<EdgeDetectionType>()
            .ToList();
    }

    // 모폴로지 연산 종류 열거형
    public enum MorphologyType
    {
        Erode,      // 침식
        Dilate,     // 팽창
        Opening,    // 열림
        Closing,    // 닫힘
        Gradient,   // 그레디언트 (팽창 - 침식)
        TopHat,     // 탑햇 (원본 - 열림)
        BlackHat    // 블랙햇 (닫힘 - 원본)
    }

    // 모폴로지 연산을 위한 파라미터 클래스
    public class MorphologyParams : AlgorithmParameters
    {
        private MorphologyType _selectedType = MorphologyType.Erode;
        public MorphologyType SelectedType
        {
            get => _selectedType;
            set 
            {
                if (_selectedType == value) return; 
                
                _selectedType = value; 
                OnPropertyChanged();
            }
        }

        // 커널 모양 (사각형, 십자형, 타원형)
        private MorphShapes _kernelShape = MorphShapes.Rect;
        public MorphShapes KernelShape
        {
            get => _kernelShape;
            set 
            {
                if (_kernelShape == value) return; 
                
                _kernelShape = value; 
                OnPropertyChanged(); 
            }
        }

        // 커널 크기
        private int _kernelSize = 3;
        public int KernelSize
        {
            get => _kernelSize;
            set
            {
                if (value < 1) value = 1;

                if (_kernelSize == value) return; 

                _kernelSize = value; 
                OnPropertyChanged();
            }
        }

        // 반복 횟수
        private int _iterations = 1;
        public int Iterations
        {
            get => _iterations;
            set
            {
                if (value < 1) value = 1;

                if (_iterations == value) return; 

                _iterations = value; 
                OnPropertyChanged();
            }
        }

        public List<MorphologyType> MorphTypeSource { get; } = Enum.GetValues(typeof(MorphologyType))
            .Cast<MorphologyType>()
            .ToList();
        public List<MorphShapes> KernelShapeSource { get; } = Enum.GetValues(typeof(MorphShapes))
            .Cast<MorphShapes>()
            .ToList();
    }

    // 이미지 피라미드 연산 종류
    public enum ImagePyramidType
    {
        GaussianDown,   // 축소 (PyrDown)
        GaussianUp,     // 확대 (PyrUp)
        Laplacian       // 라플라시안 (Original - PyrUp(PyrDown))
    }

    // 이미지 피라미드 파라미터 클래스
    public class ImagePyramidParams : AlgorithmParameters
    {
        private ImagePyramidType _selectedType = ImagePyramidType.GaussianDown;
        public ImagePyramidType SelectedType
        {
            get => _selectedType;
            set 
            {
                if (_selectedType == value) return; 

                _selectedType = value; 
                OnPropertyChanged();
            }
        }

        private BorderTypes _borderType = BorderTypes.Reflect101;
        public BorderTypes BorderType
        {
            get => _borderType;
            set 
            {
                if (_borderType == value) return; 

                _borderType = value; 
                OnPropertyChanged();
            }
        }

        public List<ImagePyramidType> PyramidTypeSource { get; } = Enum.GetValues(typeof(ImagePyramidType)).Cast<ImagePyramidType>().ToList();

        // C sharp 에서는 Default Reflect 101 만 지원
        //public List<BorderTypes> BorderTypeSource { get; } = Enum.GetValues(typeof(BorderTypes)).Cast<BorderTypes>().ToList();

        // [해결] Cv2.PyrDown/Up은 오직 BorderTypes.Reflect101(Default) 만 지원합니다.
        // 다른 타입을 선택하여 발생하는 예외를 방지하기 위해 지원되는 타입만 리스트에 담습니다.
        public List<BorderTypes> BorderTypeSource { get; } = new List<BorderTypes>
        {
            BorderTypes.Reflect101
        };
    }

}
