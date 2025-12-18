using OpenCvSharp;      // 추가.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


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
                if(_thresholdValue == value) return;
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
                if(_thresholdMax == value) return;
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
                if(_blockSize == value) return;

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
                if(_constantC == value) return;
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

}
