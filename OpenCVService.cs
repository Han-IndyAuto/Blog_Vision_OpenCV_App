using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Vision_OpenCV_App
{
    public class OpenCVService
    {

        public Mat _srcImage;         // Original Image
        public Mat _destImage;        // Processing Image

        // 화면 버퍼 : WPF의 이미지 객체
        public ImageSource _cachedOriginal;
        public ImageSource _cachedProcessed;

        // 히스토그램 결과 데이터 저장용(팝업창 전송용)
        public float[] LastHistogramData { get; private set; }
        public int LastHistogramChannel { get; private set; }


        // 생성자.
        public OpenCVService()
        {
        }


        public void CropImage(int x, int y, int w, int h)
        {
            if (_srcImage == null) return;

            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, y, w, h);

            Mat newSrc = new Mat(_srcImage, roi).Clone();
            _srcImage.Dispose();
            _srcImage = newSrc;

            if (_destImage != null) _destImage.Dispose();
            _destImage = _srcImage.Clone();

            _cachedOriginal = _srcImage.ToWriteableBitmap();
            _cachedProcessed = _destImage.ToWriteableBitmap();
        }

        public void SaveRoiImage(string filePath, int x, int y, int w, int h)
        {
            if (_srcImage == null) return;
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, y, w, h);

            // 이미지 범위 체크
            if (roi.X < 0) roi.X = 0;
            if (roi.Y < 0) roi.Y = 0;
            if (roi.X + roi.Width > _srcImage.Width) roi.Width = _srcImage.Width - roi.X;
            if (roi.Y + roi.Height > _srcImage.Height) roi.Height = _srcImage.Height - roi.Y;

            using (Mat roiMat = new Mat(_srcImage, roi))
            {
                roiMat.SaveImage(filePath);
            }
        }

        // [신규] 처리된 결과 이미지 전체 저장
        public void SaveProcessedImage(string filePath)
        {
            if (_destImage == null || _destImage.IsDisposed) return;
            _destImage.SaveImage(filePath);
        }

        public async Task LoadImageAsync(string filePath)
        {
            await Task.Run(() =>
            {
                var img = Cv2.ImRead(filePath, ImreadModes.Color);
                _srcImage = img;
                _destImage = _srcImage.Clone();
            });

            await Application.Current.Dispatcher.InvokeAsync(() => 
            {
                _cachedOriginal = _srcImage.ToWriteableBitmap();
                _cachedProcessed = _destImage.ToWriteableBitmap();
            });
        }

        public async Task<string> ProcessImageAsync(string algorithm, AlgorithmParameters parameters)
        {
            if (_srcImage == null || _srcImage.IsDisposed) return "Non Image";
            string resultMessage = "Processing Complete";

            // [추가] 히스토그램 분석인 경우 _destImage를 덮어쓰지 않기 위해 플래그 사용
            bool updateDisplayImage = true;

            await Task.Run(() => 
            {
                if (algorithm != "Histogram")
                {
                    if (_destImage != null) _destImage.Dispose();
                    _destImage = _srcImage.Clone();
                }

                switch (algorithm)
                {
                    case "Threshold":
                        if (parameters is ThresholdParams thParams)
                        {
                            using (Mat gray = new Mat())
                            {
                                Cv2.CvtColor(_srcImage, gray, ColorConversionCodes.BGR2GRAY);
                                Cv2.InRange(gray, new Scalar(thParams.ThresholdValue), new Scalar(thParams.ThresholdMax), _destImage);

                                resultMessage += $": {algorithm}";
                            }
                        }
                        break;

                    case "Otsu Threshold":
                        if (parameters is OtsuParams otsuParams)
                        {
                            using (Mat gray = new Mat())
                            {
                                Cv2.CvtColor(_srcImage, gray, ColorConversionCodes.BGR2GRAY);

                                ThresholdTypes type = otsuParams.SelectedType | ThresholdTypes.Otsu;
                                double otsuVal = Cv2.Threshold(gray, _destImage, 0, 255, type);

                                resultMessage += $": {algorithm} ({otsuParams.SelectedType}, Auto val: {otsuVal})";
                            }
                        }
                        break;

                    case "Adaptive Threshold":
                        if (parameters is AdaptiveThresholdParams adParams)
                        {
                            using (Mat gray = new Mat())
                            {
                                Cv2.CvtColor(_srcImage, gray, ColorConversionCodes.BGR2GRAY);
                                Cv2.AdaptiveThreshold(gray, _destImage, 255, 
                                    adParams.AdaptiveMethod, 
                                    adParams.ThresholdType, 
                                    adParams.BlockSize, 
                                    adParams.ConstantC);

                                resultMessage += $": {algorithm} (Block Size: {adParams.BlockSize}, C: {adParams.ConstantC})";
                            }
                        }
                        break;

                    case "Histogram":
                        if (parameters is HistogramParams histParams)
                        {
                            // 메인 화면 이미지를 업데이트 하지 않음
                            updateDisplayImage = false;

                            // 1. 소스 준비 (채널 분리)
                            Mat[] channels = Cv2.Split(_srcImage);
                            Mat source = new Mat();
                            int channelIdx = (int)histParams.Channel;

                            // 이미지가 1채널(Gray)인데 RGB 채널 요청 시 예외 처리
                            if (channels.Length == 1)
                            {
                                source = channels[0];
                                channelIdx = 0;
                            }
                            else if (channelIdx < channels.Length)
                            {
                                source = channels[channelIdx];
                            }
                            else
                            {
                                source = channels[0]; // Fallback
                            }

                            // 2. 마스크 생성
                            Mat mask = null;
                            if (histParams.MaskMode != HistogramMaskMode.None)
                            {
                                mask = new Mat(source.Size(), MatType.CV_8UC1, Scalar.All(0));
                                int w = source.Width;
                                int h = source.Height;

                                if (histParams.MaskMode == HistogramMaskMode.CenterCircle)
                                {
                                    Cv2.Circle(mask, w / 2, h / 2, Math.Min(w, h) / 3, Scalar.All(255), -1);
                                }
                                else if (histParams.MaskMode == HistogramMaskMode.LeftHalf)
                                {
                                    Cv2.Rectangle(mask, new OpenCvSharp.Rect(0, 0, w / 2, h), Scalar.All(255), -1);
                                }
                                else if (histParams.MaskMode == HistogramMaskMode.RightHalf)
                                {
                                    Cv2.Rectangle(mask, new OpenCvSharp.Rect(w / 2, 0, w / 2, h), Scalar.All(255), -1);
                                }
                            }

                            // 3. 히스토그램 계산
                            Mat hist = new Mat();
                            int[] histSize = { histParams.HistSize };
                            Rangef[] ranges = { new Rangef(histParams.RangeMin, histParams.RangeMax) };

                            Cv2.CalcHist(new[] { source }, new[] { 0 }, mask, hist, 1, histSize, ranges);

                            // 4. 데이터 정규화 (그래프 그리기용) 및 저장
                            Cv2.Normalize(hist, hist, 0, 255, NormTypes.MinMax);

                            // 팝업창 전달용 원본 데이터 복사
                            float[] rawData = new float[histParams.HistSize];
                            hist.GetArray(out rawData);
                            LastHistogramData = rawData;
                            LastHistogramChannel = channelIdx;

                            // 사용한 리소스 정리
                            if (mask != null) mask.Dispose();
                            hist.Dispose();
                            foreach (var m in channels) m.Dispose();

                            resultMessage += $": Histogram ({histParams.Channel})";
                        }
                        break;

                    // Normalize 기능 구현
                    case "Normalize":
                        if (parameters is NormalizeParams normParams)
                        {
                            // 1. [정규화 실행]
                            // 입력 이미지(_srcImage)를 정규화하여 결과 이미지(_destImage)에 저장
                            // alpha: 하한값 또는 기준값
                            // beta: 상한값 (MinMax일 때)
                            // type: 정규화 방식 (MinMax, L1, L2 등)
                            Cv2.Normalize(_srcImage, _destImage,
                                normParams.Alpha,
                                normParams.Beta,
                                normParams.NormType);

                            // 2. [히스토그램 계산]
                            // 정규화된 결과 이미지(_destImage)의 분포 변화를 확인하기 위해
                            // 히스토그램 데이터를 계산하여 팝업용 변수(LastHistogramData)에 저장합니다.
                            CalculateHistogramForPopup(_destImage);

                            resultMessage += $": Normalize ({normParams.NormType}, A:{normParams.Alpha}, B:{normParams.Beta})";
                        }
                        break;

                    case "Equalize":
                        if (parameters is EqualizeParams)
                        {
                            // 1. [그레이스케일 변환] EqualizeHist는 그레이스케일 이미지에만 적용 가능
                            using (Mat gray = new Mat())
                            {
                                Cv2.CvtColor(_srcImage, gray, ColorConversionCodes.BGR2GRAY);

                                //Cv2.ImWrite("./GrayImage.png", gray);

                                // 2. [히스토그램 평활화 실행]
                                Cv2.EqualizeHist(gray, _destImage);

                                // 3. [히스토그램 계산]
                                // 평활화된 결과 이미지(_destImage)의 분포 변화를 확인하기 위해
                                // 히스토그램 데이터를 계산하여 팝업용 변수(LastHistogramData)에 저장합니다.
                                CalculateHistogramForPopup(_destImage);

                                // 4. [결과 이미지 채널 변경]
                                // _destImage가 Gray 이미지이므로, 디스플레이를 위해 BGR로 변환합니다.
                                // (WPF ImageSource로 변환 시 BGR 형식이 일반적임)
                                Cv2.CvtColor(_destImage, _destImage, ColorConversionCodes.GRAY2BGR);

                                resultMessage += $": Equalize";
                            }
                        }
                        break;

                    case "CLAHE":
                        if (parameters is ClaheParams claheParams)
                        {
                            // 1. [그레이스케일 변환] CLAHE는 그레이스케일 이미지에 적용합니다.
                            using (Mat gray = new Mat())
                            {
                                Cv2.CvtColor(_srcImage, gray, ColorConversionCodes.BGR2GRAY);

                                // 2. [CLAHE 생성 및 설정]
                                using (var clahe = Cv2.CreateCLAHE(claheParams.ClipLimit, new OpenCvSharp.Size(claheParams.TileGridSize, claheParams.TileGridSize)))
                                {
                                    // 3. [CLAHE 적용]
                                    clahe.Apply(gray, _destImage);
                                }

                                // 4. [히스토그램 계산]
                                CalculateHistogramForPopup(_destImage);

                                // 5. [결과 이미지 채널 변경] 디스플레이용 BGR 변환
                                Cv2.CvtColor(_destImage, _destImage, ColorConversionCodes.GRAY2BGR);

                                resultMessage += $": CLAHE (Clip:{claheParams.ClipLimit}, Grid:{claheParams.TileGridSize})";
                            }
                        }
                        break;

                    // [신규] Geometric Transformation 통합 구현
                    case "Geometric Transformation":
                        if (parameters is GeometricParams geoParams)
                        {
                            // 1. 회전 및 스케일 행렬 생성 (GetRotationMatrix2D 사용)
                            // 중심점: 이미지의 정중앙
                            Point2f center = new Point2f(_srcImage.Width / 2.0f, _srcImage.Height / 2.0f);
                            Mat matrix = Cv2.GetRotationMatrix2D(center, geoParams.Angle, geoParams.Scale);

                            // 2. 이동(Translation) 변환 추가
                            // 회전 행렬의 [0, 2]는 X축 이동, [1, 2]는 Y축 이동 성분입니다.
                            // 기존 회전/스케일 변환에 사용자가 입력한 이동 값을 더해줍니다.
                            matrix.Set(0, 2, matrix.At<double>(0, 2) + geoParams.MoveX);
                            matrix.Set(1, 2, matrix.At<double>(1, 2) + geoParams.MoveY);

                            // 3. warpAffine 적용
                            // 보간법(Interpolation)도 파라미터에서 받아와 적용합니다.
                            // BorderTypes.Constant: 빈 공간은 검은색으로 채움
                            Cv2.WarpAffine(_srcImage, _destImage, matrix, _srcImage.Size(),
                                geoParams.Interpolation, BorderTypes.Constant, Scalar.All(0));

                            matrix.Dispose();
                            resultMessage += $": Geometric (Move:{geoParams.MoveX},{geoParams.MoveY} | Rot:{geoParams.Angle} | Scale:{geoParams.Scale})";
                        }
                        break;

                    case "Affine Transform":
                        if (parameters is AffineParams affineParams)
                        { 
                            // 3개의 점이 유효한지 확인
                            if(affineParams.Pt1.X == 0 && affineParams.Pt1.Y == 0 ||
                               affineParams.Pt2.X == 0 && affineParams.Pt2.Y == 0 ||
                               affineParams.Pt3.X == 0 && affineParams.Pt3.Y == 0)
                            {
                                resultMessage = "Affine: 3개의 점을 지정해 주세요.";
                                break;
                            }
                            else
                            {
                                // 입력 점 3개 설정
                                Point2f[] srcPoints = new Point2f[]
                                { 
                                    affineParams.Pt1,
                                    affineParams.Pt2,
                                    affineParams.Pt3
                                };

                                // 출력 점 3개 설정.
                                // 일반적으로 원본 이미지의 좌상단, 우상단, 좌하단으로 매핑하지만,
                                // 사용자가 선택한 3개의 점이 이루는 평행사변형 영역이 직사각형 형태로 퍼지게 됨.
                                Point2f[] dstPoionts = new Point2f[]
                                {
                                    new Point2f(0, 0),                  // 좌상단
                                    new Point2f(_srcImage.Width, 0),    // 우상단 (w, 0)
                                    new Point2f(0, _srcImage.Height)    // 좌하단 (0, h)
                                };

                                // Affine 변환 행렬 계산
                                Mat affineMatrix = Cv2.GetAffineTransform(srcPoints, dstPoionts);
                                // Affine 변환 적용
                                Cv2.WarpAffine(_srcImage, _destImage, affineMatrix, _srcImage.Size(),
                                    affineParams.Interpolation, BorderTypes.Constant, Scalar.All(0));

                                affineMatrix.Dispose();
                                resultMessage += $": Affine Transform";
                            }
                        }
                        break;
                }
            });

            // updateDisplayImage가 true일 때만 화면에 보여지는 이미지(_cachedProcessed)를 교체합니다.
            // Histogram인 경우에는 false이므로 이 블록이 실행되지 않아, 원래 이미지가 유지됩니다.
            if (updateDisplayImage)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _cachedProcessed = _destImage.ToBitmapSource();
                });
            }

            return resultMessage;
        }


        private void CleanupImages()
        {
            if (_srcImage != null) { _srcImage.Dispose(); _srcImage = null; }
            if (_destImage != null) { _destImage.Dispose(); _destImage = null; }
        }
        public void Cleanup()
        {
            CleanupImages();
        }


        // 특정 이미지의 히스토그램을 계산하여 LastHistogramData에 저장
        private void CalculateHistogramForPopup(Mat image)
        {
            if (image == null || image.IsDisposed) return;

            // Normalize 결과는 보통 Gray이거나 Color일 수 있음.
            // 편의상 0번 채널(Blue or Gray)의 히스토그램만 계산하여 보여줌.
            Mat[] channels = Cv2.Split(image);
            Mat source = channels[0];
            LastHistogramChannel = 0; // Gray/Blue

            Mat hist = new Mat();
            int[] histSize = { 256 };
            Rangef[] ranges = { new Rangef(0, 256) };

            Cv2.CalcHist(new[] { source }, new[] { 0 }, null, hist, 1, histSize, ranges);

            float[] rawData = new float[256];
            hist.GetArray(out rawData);
            LastHistogramData = rawData;

            // 리소스 정리
            hist.Dispose();
            foreach (var c in channels) c.Dispose();
        }



        public ImageSource GetOriginalImage() => _cachedOriginal;
        public ImageSource GetProcessedImage() => _cachedProcessed;
    }
}
