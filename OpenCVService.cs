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

            await Task.Run(() => 
            {
                if (_destImage != null) _destImage.Dispose();
                _destImage = _srcImage.Clone();

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

                            // 5. 결과 이미지(그래프) 생성 (배경 검정)
                            // 256 x 200 크기의 그래프 이미지 생성
                            int histW = 512;
                            int histH = 400;
                            _destImage = new Mat(histH, histW, MatType.CV_8UC3, Scalar.All(0));

                            int binW = (int)((double)histW / histSize[0]);

                            Scalar color;
                            if (channels.Length == 1) color = Scalar.Gray;
                            else if (channelIdx == 0) color = Scalar.Blue;
                            else if (channelIdx == 1) color = Scalar.Green;
                            else color = Scalar.Red;

                            for (int i = 1; i < histSize[0]; i++)
                            {
                                float val1 = rawData[i - 1];
                                float val2 = rawData[i];

                                // 값 스케일링 (이미지 높이에 맞춤)
                                int y1 = (int)(histH - (val1 / 255.0 * histH));
                                int y2 = (int)(histH - (val2 / 255.0 * histH));

                                Cv2.Line(_destImage,
                                    new OpenCvSharp.Point(binW * (i - 1), y1),
                                    new OpenCvSharp.Point(binW * i, y2),
                                    color, 2);
                            }

                            // 사용한 리소스 정리
                            if (mask != null) mask.Dispose();
                            hist.Dispose();
                            foreach (var m in channels) m.Dispose();

                            resultMessage += $": Histogram ({histParams.Channel})";
                        }
                        break;
                }
            });

            await Application.Current.Dispatcher.InvokeAsync(() => 
            {
                _cachedProcessed = _destImage.ToBitmapSource();
            });

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

        public ImageSource GetOriginalImage() => _cachedOriginal;
        public ImageSource GetProcessedImage() => _cachedProcessed;
    }
}
