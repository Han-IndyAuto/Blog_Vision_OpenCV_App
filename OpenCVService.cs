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
