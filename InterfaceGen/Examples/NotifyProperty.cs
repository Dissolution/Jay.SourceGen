using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jay.SourceGen.InterfaceGen.Examples
{
    public class NotifyProperty : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private int _id;

        public int Id
        {
            get => _id;
            set => SetField<int>(ref _id, value);
        }

        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T newValue, bool force = false, [CallerMemberName] string? propertyName = null)
        {
            if (force || (!EqualityComparer<T>.Default.Equals(field, newValue)))
            {
                OnPropertyChanging(propertyName);
                field = newValue;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        protected void OnPropertyChanging([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is not null)
            {
                this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is not null)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            this.PropertyChanging = null;
            this.PropertyChanged = null;
        }
    }
}
