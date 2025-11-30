namespace Foreman
{
    using System;

    public class ProgressInfo : ViewModel, IDisposable, IProgress<string>
    {
        private readonly Action<ProgressInfo> dispose;
        private readonly IProgress<string> progress;

        public ProgressInfo(Action<ProgressInfo> show, Action<ProgressInfo> dispose)
        {
            this.dispose = dispose;
            progress = new Progress<string>(x => Operation = x);
            show(this);
        }

        public ProgressType ProgressType
        {
            get;
            set => SetProperty(ref field, value);
        } = ProgressType.Indeterminate;

        public string? Operation
        {
            get;
            set => SetProperty(ref field, value);
        }

        public int CurrentItem
        {
            get;
            set => SetProperty(ref field, value);
        }

        public int MaximumItems
        {
            get;
            set => SetProperty(ref field, value);
        }

        public void Dispose()
        {
            dispose(this);
        }

        public void Report(string value)
        {
            progress.Report(value);
        }
    }

    public enum ProgressType
    {
        None,
        Indeterminate,
        Determinate
    }
}
