namespace Foreman
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Media.Imaging;

    public class Inserter
    {
        public Inserter(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public float RotationSpeed { get; set; }
        public BitmapSource? Icon { get; set; }

        [AllowNull]
        [field: AllowNull, MaybeNull]
        public string FriendlyName
        {
            get => !string.IsNullOrWhiteSpace(field) ? field : Name;
            set;
        }
    }
}
