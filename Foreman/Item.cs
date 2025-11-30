namespace Foreman
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Media.Imaging;

    public class Item
    {
        public string Name { get; }
        public HashSet<Recipe> Recipes { get; }

        [AllowNull]
        [field: AllowNull, MaybeNull]
        public BitmapSource Icon
        {
            get => field ?? DataCache.Current.UnknownIcon;
            set;
        }

        public LocalizationInfo? LocalizedName { get; set; }
        public string FriendlyName => DataCache.Current.GetLocalizedString(Name, LocalizedName);

        public bool IsMissingItem { get; init; }

        public Item(string name)
        {
            Name = name;
            Recipes = [];
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            var item = obj as Item;
            return item != null && this == item;
        }

        public static bool operator ==(Item? item1, Item? item2)
        {
            if (ReferenceEquals(item1, item2)) {
                return true;
            }
            if (item1 is null || item2 is null) {
                return false;
            }

            return item1.Name == item2.Name;
        }

        public static bool operator !=(Item? item1, Item? item2)
        {
            return !(item1 == item2);
        }

        public override string ToString()
        {
            return $"Item: {Name}";
        }
    }
}
