namespace Foreman
{
    using System.Diagnostics.CodeAnalysis;

    public class Language
    {
        public Language(string name)
        {
            Name = name;
        }

        public string Name { get; }

        [AllowNull]
        [field: AllowNull, MaybeNull]
        public string LocalName
        {
            get => !string.IsNullOrWhiteSpace(field) ? field : Name;
            set;
        }
    }
}
