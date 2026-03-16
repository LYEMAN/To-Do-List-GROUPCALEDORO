using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MauiApp2
{
    internal class ToDoItem : INotifyPropertyChanged
    {
        private int _id;
        private string? _name;
        private string? _notes;
        private bool _done;

        [PrimaryKey, AutoIncrement]
        public int ID
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string? Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetField(ref _notes, value);
        }

        public bool Done
        {
            get => _done;
            set => SetField(ref _done, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
