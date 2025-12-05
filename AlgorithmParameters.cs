using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

}
