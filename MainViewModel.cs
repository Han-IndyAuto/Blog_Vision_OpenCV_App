using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Vision_OpenCV_App
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private OpenCVService _cvServices;

        #region Preperties

        private string _mouseCoordinationInfo = "(X: 0, Y: 0)";
        public string MouseCoordinationInfo
        {
            get => _mouseCoordinationInfo; set
            {
                if(_mouseCoordinationInfo == value) return;
                _mouseCoordinationInfo = value;
                OnPropertyChanged();
            }
        }


        private ImageSource _displayImage;
        public ImageSource DisplayImage
        {
            // 화면이 "이미지 줘!" 하면 금고에서 꺼내줍니다.
            get => _displayImage;
            set
            {
                _displayImage = value;
                // "이미지가 바뀌었습니다!"라고 방송해서 화면이 다시 그려지게 합니다.
                OnPropertyChanged();
            }
        }

        private string _analysisResult = "Ready";
        public string AnalysisResult
        {
            get => _analysisResult;
            set { _analysisResult = value; OnPropertyChanged(); }
        }

        private bool _showOriginal;
        public bool ShowOriginal
        {
            get => _showOriginal;
            set
            {
                _showOriginal = value;
                // 체크박스 상태가 바뀜을 알림
                OnPropertyChanged();

                // 체크 상태가 바뀌면 이미지를 다시 불러옴 (원본 vs 결과)
                // [중요] 체크박스를 껐다 켰다 할 때마다 즉시 이미지를 바꿔 끼워줍니다.
                // (체크됨: 원본 보여줘 / 체크해제: 결과 보여줘)
                UpdateDisplay();
            }
        }

        // 작업 진행 상태를 나타내는 속성. (로딩 바 표시용)
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> AlgorithmList { get; set; }

        private string _selectedAlgorithm;
        public string SelectedAlgorithm
        {
            get => _selectedAlgorithm;
            set
            {
                _selectedAlgorithm = value;
                OnPropertyChanged();

                // [핵심 로직] 
                // 사용자가 "이진화"를 선택하면 -> 이진화용 슬라이더 설정(Params)을 만듭니다.
                // 사용자가 "모폴로지"를 선택하면 -> 모폴로지용 설정(Params)을 만듭니다.
                // 알고리즘 선택 시 해당 파라미터 객체 생성
                CreateParametersForAlgorithm(value);

                // 알고리즘을 바꾸면 즉시 한번 실행.
                //ApplyAlgorithm(null); // 선택 즉시 적용
            }
        }

        private AlgorithmParameters _currentParameters;
        public AlgorithmParameters CurrentParameters
        {
            get => _currentParameters;
            set { _currentParameters = value; OnPropertyChanged(); }  // 원본
        }
        #endregion

        public MainViewModel()
        {
            _cvServices = new OpenCVService();

            AlgorithmList = new ObservableCollection<string>
            {
                //"ROI Selection (영역 설정)",
                //"Gray 처리", // (OpenCV 서비스 로직에서 예외처리 혹은 구현 필요, 여기선 생략)

                "Threshold",
                "Otsu Threshold",
                "Adaptive Threshold",
                "Histogram",
                "Normalize",
                "Equalize",
                "CLAHE"

                //"Morphology (모폴로지)",
                //"Edge Detection (엣지 검출)",
                //"Blob Analysis (블롭 분석)",
                //"TemplateMatching (TM)",
                //"Geometric Model Finder (GMF)" // 이름 유지, 내부 로직은 Template Matching
            };
        }

        private void UpdateDisplay()
        {
            if (ShowOriginal)
                // 체크박스가 켜져있으면 -> MilService에게 "원본 내놔"라고 함
                DisplayImage = _cvServices.GetOriginalImage();
            else
                // 꺼져있으면 -> "결과물 내놔"라고 함
                DisplayImage = _cvServices.GetProcessedImage();
        }

        private void CreateParametersForAlgorithm(string algoName)
        {
            // 선택된 이름에 따라 적절한 설정 클래스 생성
            switch (algoName)
            {
                case "Threshold":
                    // 이진화 설정을 담을 그릇을 새로 만듭니다. (기본값 128 등 포함)
                    CurrentParameters = new ThresholdParams();
                    break;

                case "Adaptive Threshold":
                    CurrentParameters = new AdaptiveThresholdParams();
                    break;

                case "Otsu Threshold":
                    // Otsu는 별도 설정이 필요 없으므로 null
                    CurrentParameters = new OtsuParams();
                    break;

                case "Histogram":
                    CurrentParameters = new HistogramParams();
                    break;

                case "Normalize":
                    CurrentParameters = new NormalizeParams();
                    break;

                case "Equalize":
                    CurrentParameters = new EqualizeParams();
                    break;

                case "CLAHE":
                    CurrentParameters = new ClaheParams();
                    break;

                default:
                    CurrentParameters = null; // 설정이 필요 없는 경우
                    break;
            }
        }


        private async void LoadImage(object obj)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Image Files|*.bmp;*.jpg;*.png" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    IsBusy = true; // 로딩 시작 알림 (UI 프리징 방지)
                    AnalysisResult = "Loading Image...";

                    // 비동기 함수 호출 (await로 기다림, UI 스레드는 자유로움)
                    await _cvServices.LoadImageAsync(dlg.FileName);

                    ShowOriginal = true;
                    UpdateDisplay();
                    AnalysisResult = "Image Loaded Successfully.";
                }
                catch (Exception ex)
                {
                    AnalysisResult = "Load Error: " + ex.Message;
                }
                finally
                {
                    IsBusy = false; // 로딩 종료
                }
            }
        }

        private async void ApplyAlgorithm(object obj)
        {
            if (string.IsNullOrEmpty(SelectedAlgorithm)) return;
            if (IsBusy) return; // 작업 중 중복 실행 방지

            try
            {
                IsBusy = true;
                AnalysisResult = "Processing...";

                // 비동기 처리 호출
                string result = await _cvServices.ProcessImageAsync(SelectedAlgorithm, CurrentParameters);

                if (result == "This Image is Gray Image.")
                {
                    MessageBox.Show("Gray 영상입니다.", "Gray Image", MessageBoxButton.OK);
                }

                AnalysisResult = result;
                ShowOriginal = false;
                UpdateDisplay();

                // 히스토그램 알고리즘의 경우, 팝업 윈도우 표시
                //if(SelectedAlgorithm == "Histogram" && _cvServices.LastHistogramData != null)
                if((SelectedAlgorithm == "Histogram" || SelectedAlgorithm == "Normalize" || SelectedAlgorithm == "Equalize" || SelectedAlgorithm == "CLAHE") 
                    && _cvServices.LastHistogramData != null)
                {
                    HistogramWindow histWin = new HistogramWindow(_cvServices.LastHistogramData, _cvServices.LastHistogramChannel);
                    histWin.Owner = Application.Current.MainWindow; // 부모 창 설정
                    histWin.Show();
                }
            }
            catch (Exception ex)
            {
                AnalysisResult = "처리 중 에러: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }


        public void CropImage(int x, int y, int w, int h)
        {
            try
            {
                _cvServices.CropImage(x, y, w, h);
                ShowOriginal = true; // 잘린 이미지가 원본이므로 원본 보기로 전환.
                UpdateDisplay();
                AnalysisResult = $"이미지 자르기 완료 (크기:{w} x {h})";
            }
            catch (Exception ex)
            {
                // 예외 처리: 로그 기록 또는 사용자 알림
                Console.WriteLine($"AnalysisResult = Error: {ex.Message}");
            }
        }

        public void SaveRoiImage(string path, int x, int y, int w, int h)
        {
            try
            {
                _cvServices.SaveRoiImage(path, x, y, w, h);
                AnalysisResult = $"ROI 저장 완료: {path}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AnalysisResult = Error: {ex.Message}");
            }
        }


        // 프로그램 종료 시 호출되어 메모리를 청소합니다.
        public void Cleanup() => _cvServices.Cleanup();


        // --- Commands ---
        // 버튼과 연결되는 끈(Command)입니다.
        public ICommand LoadImageCommand => new RelayCommand(LoadImage);
        public ICommand ApplyAlgorithmCommand => new RelayCommand(ApplyAlgorithm);

    }

    public class RelayCommand : ICommand
    {
        // [1] 실제 실행할 함수를 담아두는 변수
        // 나중에 버튼을 누르면 어떤 함수를 실행해야 하는지 기억해두는 곳입니다.
        // Action<object>: "매개변수 하나를 받고, 리턴값이 없는 함수"라는 뜻
        private readonly Action<object> _execute;

        // [2] 생성자 (심부름 시키기)
        // 생성자입니다. RelayCommand를 처음 만들 때 호출
        // 외부(MainViewModel)에서 "야, 버튼 누르면 LoadImage 함수 실행해!"라고 전달해주면, 그걸 받아서 위의 _execute 변수에 저장
        // 코드 예시: new RelayCommand(LoadImage) -> _execute에 LoadImage가 저장됨.
        public RelayCommand(Action<object> execute) => _execute = execute;

        // [3] 실행 가능 여부 확인 (버튼 활성화/비활성화 결정)
        // 의미: "지금 이 명령을 실행할 수 있나요?"라고 버튼이 물어보는 함수
        // 이 함수가 true를 반환하면 -> 버튼이 활성화(Enable) 됩니다.
        // 이 함수가 false를 반환하면 -> 버튼이 자동으로 비활성화(Disable, 회색) 됩니다.
        // 현재코드에서는 true로 되어 있으므로, **"언제든지 실행 가능해!"**라고 답하는 것입니다. (버튼이 항상 눌리는 상태)
        public bool CanExecute(object parameter) => true;

        // [4] 실제 실행 (버튼 눌렀을 때 동작)
        // 의미: "실제 실행(Execute)!" 입니다.
        // 역할: 사용자가 버튼을 클릭하면, WPF가 자동으로 이 함수를 호출합니다.
        // 그러면 이 함수는 아까 [1]번에 저장해뒀던 진짜 함수(_execute)를 대리 호출(Relay) 해줍니다.
        // 결과적으로 LoadImage()가 실행
        public void Execute(object parameter) => _execute(parameter);

        // [5] 상태 변경 알림 이벤트 (필수 규칙)
        // 의미: ICommand 인터페이스(규칙)를 지키기 위해 반드시 있어야 하는 이벤트
        // 역할: "실행 가능 여부(CanExecute의 결과)가 바뀌었으니 버튼 상태를 다시 확인해봐!"라고 UI에게 알려주는 신호입니다.
        // 현재 코드: 이 심플 버전에서는 CanExecute가 항상 true이므로, 이 이벤트를 따로 사용하지 않고 선언만 해둔 것입니다.
        public event EventHandler CanExecuteChanged;
    }
}
