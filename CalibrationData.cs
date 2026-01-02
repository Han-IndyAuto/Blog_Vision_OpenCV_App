using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using OpenCvSharp;

namespace Vision_OpenCV_App
{
    public class CalibrationData
    {
        public double[] CameraMatrix { get; set; } = Array.Empty<double>();
        public double[] DistCoeffs { get; set; } = Array.Empty<double>();
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        public void Save(string path)
        {
            string json = JsonSerializer.Serialize(this);
            File.WriteAllText(path, json);
        }

        // ?의 의미: CalibrationData?는 "이 변수에는 데이터가 들어있을 수도 있고, null일 수도 있다"는 뜻입니다 (Nullable Reference Type).
        public static CalibrationData? Load(string path)
        {
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return  JsonSerializer.Deserialize<CalibrationData>(json);
        }
    }
}
