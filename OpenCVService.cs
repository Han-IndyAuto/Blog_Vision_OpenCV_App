using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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

        // Calibration Data memory cache
        private CalibrationData _calibData;
        private Mat _cameraMatrixMat;
        private Mat _distCoeffsMat;

        private const string CalibFileName = "calibration.json";

        public bool IsCalibrated => _calibData != null;


        // 생성자.
        public OpenCVService()
        {
            LoadCalibrationData(CalibFileName);
        }

        // 1. 캘리브레이션 실행 함수
        public double RunCalibration(List<string> filePaths, int patternW, int patternH)
        {
            // 체스보드 코너 좌표들을 담을 리스트
            List<Mat> objectPoints = new List<Mat>();
            List<Mat> imagePoints = new List<Mat>();

            // 체스보드의 3D 좌표 (z=0) 생성
            List<Point3f> objP = new List<Point3f>();
            for (int i = 0; i < patternH; i++)
            {
                for (int j = 0; j < patternW; j++)
                    objP.Add(new Point3f(j, i, 0));
            }
            Mat objPointsMat = InputArray.Create(objP).GetMat();

            OpenCvSharp.Size imageSize = new OpenCvSharp.Size();
            int successCount = 0;
            OpenCvSharp.Size patternSize = new OpenCvSharp.Size(patternW, patternH);

            foreach (var path in filePaths)
            {
                using (Mat img = Cv2.ImRead(path, ImreadModes.Grayscale))
                {
                    if (img.Empty()) continue;
                    imageSize = img.Size();

                    Point2f[] corners;
                    // 체스보드 코너 찾기
                    bool found = Cv2.FindChessboardCorners(img, patternSize, out corners);

                    if (found)
                    {
                        // 코너 위치 정밀화 (SubPixel)
                        Cv2.CornerSubPix(img, corners, new OpenCvSharp.Size(11, 11), new OpenCvSharp.Size(-1, -1),
                            new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 30, 0.1));

                        imagePoints.Add(InputArray.Create(corners).GetMat());
                        objectPoints.Add(objPointsMat);
                        successCount++;
                    }
                }
            }

            if (successCount < 1) return -1.0; // 실패 시 -1 리턴

            // 카메라 캘리브레이션 수행
            Mat camMat = new Mat();
            Mat distCoeffs = new Mat();
            Mat[] rvecs, tvecs;

            double rms = Cv2.CalibrateCamera(objectPoints, imagePoints, imageSize, camMat, distCoeffs,
                out rvecs, out tvecs);

            // 결과 저장 (1차원 배열 변환)
            double[] cameraMatrixArray = new double[9];
            double[] distCoeffsArray = new double[distCoeffs.Rows * distCoeffs.Cols];

            Marshal.Copy(camMat.Data, cameraMatrixArray, 0, cameraMatrixArray.Length);
            Marshal.Copy(distCoeffs.Data, distCoeffsArray, 0, distCoeffsArray.Length);

            _calibData = new CalibrationData
            {
                CameraMatrix = cameraMatrixArray,
                DistCoeffs = distCoeffsArray,
                ImageWidth = imageSize.Width,
                ImageHeight = imageSize.Height
            };

            // 외부 CalibrationData 클래스 사용하여 저장
            _calibData.Save(CalibFileName);

            // Mat 객체 갱신
            UpdateCalibrationMatrices();

            return rms; // RMS 오차 반환
        }

        private void LoadCalibrationData(string path)
        {
            var data = CalibrationData.Load(path);
            if (data != null)
            {
                _calibData = data;
                UpdateCalibrationMatrices();
            }
        }

        private void UpdateCalibrationMatrices()
        {
            if (_calibData == null) return;

            // 1. 카메라 매트릭스 (3x3)
            _cameraMatrixMat = new Mat(3, 3, MatType.CV_64FC1);
            Marshal.Copy(_calibData.CameraMatrix, 0, _cameraMatrixMat.Data, _calibData.CameraMatrix.Length);

            // 2. 왜곡 계수 (Nx1)
            _distCoeffsMat = new Mat(_calibData.DistCoeffs.Length, 1, MatType.CV_64FC1);
            Marshal.Copy(_calibData.DistCoeffs, 0, _distCoeffsMat.Data, _calibData.DistCoeffs.Length);
        }


        // 2. 왜곡 보정 적용 함수
        // isCorrected가 true면 Undistort 수행, false면 원본 리턴
        public void ApplyLensCorrection(bool isCorrected)
        {
            if (_srcImage == null) return;

            if (isCorrected && IsCalibrated && _cameraMatrixMat != null && _distCoeffsMat != null)
            {
                Mat undistorted = new Mat();
                // 원본 매트릭스 사용하여 비율 유지
                Cv2.Undistort(_srcImage, undistorted, _cameraMatrixMat, _distCoeffsMat, _cameraMatrixMat);

                if (_destImage != null) _destImage.Dispose();
                _destImage = undistorted;
            }
            else
            {
                if (_destImage != null) _destImage.Dispose();
                _destImage = _srcImage.Clone();
            }
            UpdateCachedImages();
        }

        // 화면 갱신용 헬퍼
        private void UpdateCachedImages()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 원본은 그대로 유지하거나 필요 시 업데이트
                // _cachedOriginal = _srcImage.ToWriteableBitmap(); 

                // 결과 이미지 갱신
                _cachedProcessed = _destImage.ToWriteableBitmap();
            });
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

        public async Task<string> ProcessImageAsync(string algorithm, AlgorithmParameters? parameters)
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

                    case "Perspective Transform":
                        if (parameters is PerspectiveParams perspParams)
                        {
                            // 4개의 점이 유효한지 확인 (간단히 Pt4까지 찍혔는지 확인)
                            if (perspParams.Pt1 == new Point2f(0, 0) && perspParams.Pt4 == new Point2f(0, 0))
                            {
                                resultMessage = "Perspective: 4개의 점을 지정해주세요.";
                            }
                            else
                            {
                                // 1. 입력 점 4개 (사용자가 찍은 R, G, B, Y)
                                Point2f[] srcPoints = new Point2f[]
                                {
                                    perspParams.Pt1,
                                    perspParams.Pt2,
                                    perspParams.Pt3,
                                    perspParams.Pt4
                                };

                                // 2. 출력 점 4개 (매핑될 위치)
                                // 일반적인 순서: 좌상 -> 우상 -> 우하 -> 좌하 (Z 모양 혹은 시계방향)
                                // 사용자가 찍는 순서도 이와 같다고 가정합니다.
                                Point2f[] dstPoints = new Point2f[]
                                {
                                    new Point2f(0, 0),                        // 좌상단
                                    new Point2f(_srcImage.Width, 0),          // 우상단
                                    new Point2f(_srcImage.Width, _srcImage.Height), // 우하단 (Affine과 다름!)
                                    new Point2f(0, _srcImage.Height)          // 좌하단
                                };

                                // 3. Perspective 변환 행렬 계산 (3x3 행렬)
                                Mat perspectiveMatrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);

                                // 4. warpPerspective 적용
                                Cv2.WarpPerspective(_srcImage, _destImage, perspectiveMatrix, _srcImage.Size(),
                                    perspParams.Interpolation, BorderTypes.Constant, Scalar.All(0));

                                perspectiveMatrix.Dispose();
                                resultMessage += $": Perspective Applied";
                            }
                        }
                        break;


                    case "Lens Distortion (Remap)":

#if ManualCalculate
                        #region Manual Calculation (Old)
                        if (parameters is RemapParams remapParams)
                        {
                            int w = _srcImage.Width;
                            int h = _srcImage.Height;

                            Mat mapX = new Mat(_srcImage.Size(), MatType.CV_32FC1);
                            Mat mapY = new Mat(_srcImage.Size(), MatType.CV_32FC1);

                            var indexerX = mapX.GetGenericIndexer<float>();
                            var indexerY = mapY.GetGenericIndexer<float>();

                            // --- 모드 분기 ---
                            if (remapParams.Mode == DistortionMode.SinCosWave)
                            {
                                // [모드 1] 기존 Sin/Cos 물결
                                double wavelength = remapParams.Wavelength;
                                double amplitude = remapParams.Amplitude;
                                double phase = remapParams.Phase * (Math.PI / 180.0);

                                Parallel.For(0, h, y =>
                                {
                                    for (int x = 0; x < w; x++)
                                    {
                                        float newX = (float)(x + amplitude * Math.Sin(y / wavelength + phase));
                                        float newY = (float)(y + amplitude * Math.Cos(x / wavelength + phase));
                                        indexerX[y, x] = newX;
                                        indexerY[y, x] = newY;
                                    }
                                });
                                resultMessage += $": Wave (W:{wavelength}, A:{amplitude})";
                            }
                            else if (remapParams.Mode == DistortionMode.LensSimulate)
                            {
                                // [모드 2] 볼록/오목 렌즈
                                float cx = remapParams.Center.X;
                                float cy = remapParams.Center.Y;
                                double exp = remapParams.Exponent;
                                double maxRadius = (Math.Min(w, h) / 2.0) * remapParams.Scale;
                                if (maxRadius < 1.0) maxRadius = 1.0;

                                Parallel.For(0, h, y =>
                                {
                                    for (int x = 0; x < w; x++)
                                    {
                                        double dx = x - cx;
                                        double dy = y - cy;
                                        double r = Math.Sqrt(dx * dx + dy * dy);

                                        if (r < maxRadius)
                                        {
                                            double rNorm = r / maxRadius;
                                            double rNewNorm = Math.Pow(rNorm, exp);
                                            double rNew = rNewNorm * maxRadius;
                                            double scaleFactor = (r == 0) ? 1.0 : (rNew / r);

                                            indexerX[y, x] = (float)(cx + dx * scaleFactor);
                                            indexerY[y, x] = (float)(cy + dy * scaleFactor);
                                        }
                                        else
                                        {
                                            indexerX[y, x] = x;
                                            indexerY[y, x] = y;
                                        }
                                    }
                                });
                                resultMessage += $": Lens (Exp:{exp:F2}, Scale:{remapParams.Scale:P0})";
                            }

                            // Remap 적용 (공통)
                            Cv2.Remap(_srcImage, _destImage, mapX, mapY,
                                remapParams.Interpolation, BorderTypes.Constant, Scalar.All(0));

                            mapX.Dispose();
                            mapY.Dispose();
                        }
                        #endregion
#else
                        #region Polar Method (New)
                        if (parameters is RemapParams remapParams)
                        {
                            int w = _srcImage.Width;
                            int h = _srcImage.Height;

                            // Remap을 위한 최종 맵 (32비트 float 필수)
                            Mat mapX = new Mat(_srcImage.Size(), MatType.CV_32FC1);
                            Mat mapY = new Mat(_srcImage.Size(), MatType.CV_32FC1);

                            // --- 모드 분기 ---
                            if (remapParams.Mode == DistortionMode.SinCosWave)
                            {
                                // [기존 방식 유지] Sin/Cos 물결 효과
                                var indexerX = mapX.GetGenericIndexer<float>();
                                var indexerY = mapY.GetGenericIndexer<float>();

                                double wavelength = remapParams.Wavelength;
                                double amplitude = remapParams.Amplitude;
                                double phase = remapParams.Phase * (Math.PI / 180.0);

                                Parallel.For(0, h, y =>
                                {
                                    for (int x = 0; x < w; x++)
                                    {
                                        float newX = (float)(x + amplitude * Math.Sin(y / wavelength + phase));
                                        float newY = (float)(y + amplitude * Math.Cos(x / wavelength + phase));
                                        indexerX[y, x] = newX;
                                        indexerY[y, x] = newY;
                                    }
                                });
                                resultMessage += $": Wave (W:{wavelength}, A:{amplitude})";
                            }
                            else if (remapParams.Mode == DistortionMode.LensSimulate)
                            {
                                // [신규 방식 적용] CartToPolar & PolarToCart 사용
                                float cx = remapParams.Center.X;
                                float cy = remapParams.Center.Y;
                                double exp = remapParams.Exponent;
                                double maxRadius = (Math.Min(w, h) / 2.0) * remapParams.Scale;
                                if (maxRadius < 1.0) maxRadius = 1.0;

                                // 1. 상대 좌표(Relative Coordinates) 생성
                                // mapX, mapY에 (x - cx), (y - cy) 값을 먼저 채웁니다.
                                var indexerX = mapX.GetGenericIndexer<float>();
                                var indexerY = mapY.GetGenericIndexer<float>();

                                Parallel.For(0, h, y =>
                                {
                                    for (int x = 0; x < w; x++)
                                    {
                                        // 클릭한 직교좌표를 극좌표 변환을 위해 중심점 기준의 상대좌표로 변경
                                        indexerX[y, x] = x - cx;
                                        indexerY[y, x] = y - cy;
                                    }
                                });

                                // 2. 직교좌표(x, y) -> 극좌표(mag, ang) 변환
                                // mag: 거리(r), ang: 각도(theta)
                                using (Mat mag = new Mat())
                                using (Mat ang = new Mat())
                                {
                                    Cv2.CartToPolar(mapX, mapY, mag, ang);

                                    // 3. 거리(Magnitude)에 왜곡 효과 적용
                                    // 조건부 비선형 변환이므로 병렬 루프로 처리
                                    var indexerMag = mag.GetGenericIndexer<float>();

                                    Parallel.For(0, h, y =>
                                    {
                                        for (int x = 0; x < w; x++)
                                        {
                                            float r = indexerMag[y, x];

                                            // 설정된 반지름 안쪽만 왜곡
                                            if (r < maxRadius)
                                            {
                                                // 정규화: 0.0 ~ 1.0
                                                float rNorm = r / (float)maxRadius;

                                                // 왜곡 적용: r_new = r_norm ^ exp
                                                // (Inverse Mapping 원리에 따라 exp > 1이면 확대(볼록), exp < 1이면 축소(오목))
                                                float rNewNorm = (float)Math.Pow(rNorm, exp);

                                                // 실제 거리로 복원
                                                float rNew = rNewNorm * (float)maxRadius;

                                                indexerMag[y, x] = rNew;
                                            }
                                        }
                                    });

                                    // 4. 극좌표(mag, ang) -> 직교좌표(mapX, mapY) 복원
                                    // 이 시점에서 mapX, mapY에는 왜곡된 상대 좌표가 들어갑니다.
                                    Cv2.PolarToCart(mag, ang, mapX, mapY);
                                }

                                // 5. 절대 좌표로 변환 (중심점 더하기)
                                // Remap 함수는 이미지 상의 절대 좌표(0~Width)를 필요로 합니다.
                                // Cv2.Add를 사용하여 행렬 전체에 스칼라 값을 더합니다. (루프보다 빠르고 간결함)
                                Cv2.Add(mapX, new Scalar(cx), mapX);
                                Cv2.Add(mapY, new Scalar(cy), mapY);

                                resultMessage += $": Lens (Polar Method, Exp:{exp:F2})";
                            }

                            // 6. Remap 적용 (공통)
                            Cv2.Remap(_srcImage, _destImage, mapX, mapY,
                                remapParams.Interpolation, BorderTypes.Constant, Scalar.All(0));

                            mapX.Dispose();
                            mapY.Dispose();
                        }
                        #endregion
#endif
                        break;

                    case "Camera Calibration":
                        if (IsCalibrated && _cameraMatrixMat != null && _distCoeffsMat != null)
                        {
                            // Apply 버튼 클릭 시: 왜곡 보정 수행
                            Cv2.Undistort(_srcImage, _destImage, _cameraMatrixMat, _distCoeffsMat, _cameraMatrixMat);
                            resultMessage = "Undistort Applied (Calibration Data Used)";
                        }
                        else
                        {
                            resultMessage = "Error: No Calibration Data. Please calibrate first.";
                        }
                        break;

                    case "Manual Filter":
                        if (parameters is ManualFilterParams filterParams)
                        {
                            // kernel 행렬 생성 : Mat 클래스의 생성자가 public이 아니라서 외부에서 직접 호출 할수 없어 using Marshal로 데이터 복사.
                            // 먼저 Mat 객체 (kernel)를 생성한 후, Marshal.Copy를 사용하여 float 배열 데이터를 Mat의 Data 포인터로 복사합니다.
                            using (Mat kernel = new Mat(3, 3, MatType.CV_32F))
                            {
                                Marshal.Copy(filterParams.KernelData, 0, kernel.Data, filterParams.KernelData.Length);
                                OpenCvSharp.Point anchor = new OpenCvSharp.Point(filterParams.AnchorX, filterParams.AnchorY);

                                // ddepth 결정 (Edge 검출 시 음수 표현을 위해 16S 사용)
                                MatType ddepth = -1; // 기본값
                                if(filterParams.SelectedKernelType == FilterKernelType.Edge)
                                    ddepth = MatType.CV_16S;

                                using (Mat tempDst = new Mat())
                                {
                                    Cv2.Filter2D(_srcImage, tempDst, ddepth, kernel, anchor, filterParams.Delta, filterParams.BorderType);

                                    // 결과 처리 (음수 값 처리)
                                    if(ddepth == MatType.CV_16S)
                                        Cv2.ConvertScaleAbs(tempDst, _destImage);
                                    else
                                        tempDst.CopyTo(_destImage);
                                }
                            }
                            resultMessage += $": {filterParams.SelectedKernelType} Filter";
                        }
                        break;

                    case "Auto Filter":
                        if (parameters is AutoFilterParams autoParams)
                        {
                            int k = autoParams.KernelSize;

                            switch (autoParams.SelectedFilterType)
                            {
                                case AutoFilterType.AverageBlur:
                                    // 가장 기본적인 평균 블러
                                    Cv2.Blur(_srcImage, _destImage, new OpenCvSharp.Size(k, k));
                                    resultMessage += $": Blur (Kernel: {k}x{k})";
                                    break;

                                case AutoFilterType.BoxFilter:
                                    // BoxFilter (normalized=true이면 Blur와 동일)
                                    // ddepth = -1 (입력과 동일)
                                    Cv2.BoxFilter(_srcImage, _destImage, -1, new OpenCvSharp.Size(k, k), normalize: true);
                                    resultMessage += $": BoxFilter (Kernel: {k}x{k})";
                                    break;

                                case AutoFilterType.GaussianBlur:
                                    // Gaussian Blur
                                    Cv2.GaussianBlur(_srcImage, _destImage, new OpenCvSharp.Size(k, k),
                                        autoParams.SigmaX, autoParams.SigmaY);
                                    resultMessage += $": Gaussian (Kernel: {k}x{k}, Sigma: {autoParams.SigmaX:F1}/{autoParams.SigmaY:F1})";
                                    break;

                                case AutoFilterType.MedianBlur:
                                    // Median Blur (소금후추 노이즈 제거에 탁월)
                                    // ksize는 1보다 큰 홀수여야 함
                                    if (k % 2 == 0) k++;
                                    if (k < 1) k = 1;

                                    Cv2.MedianBlur(_srcImage, _destImage, k);
                                    resultMessage += $": Median (Kernel: {k})";
                                    break;

                                case AutoFilterType.BilateralFilter:
                                    // Bilateral Filter (엣지 보존 스무딩)
                                    // 매우 느릴 수 있으므로 주의. srcImage가 Color여야 함.
                                    Cv2.BilateralFilter(_srcImage, _destImage,
                                        autoParams.Diameter,
                                        autoParams.SigmaColor,
                                        autoParams.SigmaSpace);
                                    resultMessage += $": Bilateral (d:{autoParams.Diameter}, sColor:{autoParams.SigmaColor}, sSpace:{autoParams.SigmaSpace})";
                                    break;
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
