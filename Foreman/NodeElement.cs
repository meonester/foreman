namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using Controls;
    using Foreman.Extensions;
    using Views;

    public class NodeElement : GraphElement, IPlacedElement, IContextElement
    {
        private static Color RecipeColor => Color.FromArgb(0xFF, 0xBE, 0xD9, 0xD4);
        private static Color PassthroughColor => Color.FromArgb(0xFF, 0xBE, 0xD9, 0xD4);
        private static Color SupplyColor => Color.FromArgb(0xFF, 0xF9, 0xED, 0xC3);
        private static Color ConsumerColor => Color.FromArgb(0xFF, 0xE7, 0xD6, 0xE0);
        private static Color MissingColor => Color.FromArgb(0xFF, 0xFF, 0x7F, 0x6B);

        private static Popup? nodeRatePopup;

        public NodeElement(ProductionNode displayedNode, ProductionGraphViewModel parent)
        {
            Parent = parent;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            DisplayedNode = displayedNode;
            Initialize(displayedNode);

            BackgroundColor = DisplayedNode switch {
                ConsumerNode consumer => consumer.ConsumedItem.IsMissingItem ? MissingColor : SupplyColor,
                SupplyNode supplier => supplier.SuppliedItem.IsMissingItem ? MissingColor : ConsumerColor,
                RecipeNode recipe => recipe.BaseRecipe.IsMissingRecipe ? MissingColor : RecipeColor,
                PassthroughNode passthrough => passthrough.PassedItem.IsMissingItem
                    ? MissingColor
                    : PassthroughColor,
                _ => throw new ArgumentException("No branch for node: " + DisplayedNode)
            };
        }

        public ProductionGraphViewModel Parent { get; }

        public ProductionNode DisplayedNode { get; }

        public Point Position
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    OnPositionChanged();
            }
        }

        public Size RenderSize
        {
            get;
            set => SetProperty(ref field, value);
        }

        public double RenderWidth
        {
            get => RenderSize.Width;
            set => RenderSize = new Size(value, RenderSize.Height);
        }

        public double RenderHeight
        {
            get => RenderSize.Height;
            set => RenderSize = new Size(RenderSize.Width, value);
        }

        public ImageSource? Icon
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string DisplayedNumber
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public string Text
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public string? BalloonText
        {
            get;
            set => SetProperty(ref field, value);
        }

        public Color BackgroundColor
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool ShowText
        {
            get;
            set => SetProperty(ref field, value);
        } = true;

        public bool ShowNumber
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool ShowIcon
        {
            get;
            set => SetProperty(ref field, value);
        }

        public override bool IsDraggable => true;
        public override bool IsSelectable => true;

        public ObservableCollection<Pin> Inputs { get; } = [];
        public ObservableCollection<Pin> Outputs { get; } = [];

        public IEnumerable<Pin> Pins => Inputs.Union(Outputs);
        public IEnumerable<Connector> Connectors => Pins.SelectMany(x => x.Connectors);

        public Pin? GetInputFor(Item item)
        {
            return Inputs.FirstOrDefault(x => x.Item == item);
        }

        public Pin? GetOutputFor(Item item)
        {
            return Outputs.FirstOrDefault(x => x.Item == item);
        }

        private void Initialize(ProductionNode node)
        {
            foreach (var input in node.Inputs)
                Inputs.Add(new Pin(PinKind.Input, input, this));
            foreach (var output in node.Outputs)
                Outputs.Add(new Pin(PinKind.Output, output, this));

            foreach (var pin in Pins)
                pin.ConnectionChanged += (_, _) => UpdatePinOrder();
        }

        private void OnPositionChanged()
        {
            UpdatePinOrder();
            foreach (var pin in Pins) {
                PinKind connectedKind = pin.Kind == PinKind.Input ? PinKind.Output : PinKind.Input;

                foreach (var node in pin.GetConnectedNodes()) {
                    if (node != this) {
                        node.UpdatePinOrder(connectedKind);
                    }
                }
            }
        }

        public void Update()
        {
            if (DisplayedNode is SupplyNode supplyNode) {
                if (!Parent.ShowMiners) {
                    if (supplyNode.SuppliedItem.IsMissingItem)
                        Text = $"Item not loaded! ({supplyNode.DisplayName})";
                    else
                        Text = "Input: " + supplyNode.SuppliedItem.FriendlyName;
                } else {
                    Text = "";
                }
                ShowText = !Parent.ShowMiners;
                ShowIcon = Parent.ShowMiners;
                if (ShowIcon) {
                    var permutations = supplyNode.GetMinimumMiners();
                    var permutation = permutations.FirstOrDefault();
                    if (permutation.Key != null) {
                        Icon = CreateIcon(permutation.Key);
                        DisplayedNumber = permutation.Value.ToString("F2");
                    } else {
                        Icon = null;
                        DisplayedNumber = string.Empty;
                    }

                    BalloonText = CreateDetails(supplyNode, permutations);
                }
            } else if (DisplayedNode is ConsumerNode consumerNode) {
                if (consumerNode.ConsumedItem.IsMissingItem)
                    Text = $"Item not loaded! ({consumerNode.DisplayName})";
                else
                    Text = "Output: " + consumerNode.ConsumedItem.FriendlyName;
                ShowText = !string.IsNullOrEmpty(Text);
                ShowIcon = false;
            } else if (DisplayedNode is RecipeNode recipeNode) {
                if (!Parent.ShowAssemblers) {
                    if (recipeNode.BaseRecipe.IsMissingRecipe)
                        Text = $"Recipe not loaded! ({recipeNode.DisplayName})";
                    else
                        Text = "Recipe: " + recipeNode.BaseRecipe.FriendlyName;
                } else {
                    Text = "";
                }
                ShowText = !Parent.ShowAssemblers;
                ShowIcon = Parent.ShowAssemblers;
                if (ShowIcon) {
                    var permutations = recipeNode.GetAssemblers();
                    var permutation = permutations.FirstOrDefault();
                    if (permutation.Key != null) {
                        Icon = CreateIcon(permutation.Key);
                        if (permutation.Value > 0)
                            DisplayedNumber = permutation.Value.ToString("F2");
                        else
                            DisplayedNumber = string.Empty;
                    } else {
                        Icon = null;
                        DisplayedNumber = string.Empty;
                    }

                    BalloonText = CreateDetails(recipeNode, permutations);
                }
            }

            ShowNumber = ShowIcon;

            foreach (Pin pin in Pins) {
                pin.Label = GetPinLabel(pin.Item, pin.Kind);
                pin.TextColor = ChoosePinTextColor(pin.Item, pin.Kind);
                pin.FillColor = ChoosePinFillColor(pin.Item, pin.Kind);
            }
        }

        private static ImageSource? CreateIcon(MachinePermutation permutation)
        {
            var assemblerIcon = permutation.Assembler.Icon;
            if (assemblerIcon == null)
                return null;

            var iconSize = Math.Min(assemblerIcon.Width, assemblerIcon.Height);

            var dg = new DrawingGroup();
            dg.Children.Add(
                new ImageDrawing(assemblerIcon, new Rect(new Size(assemblerIcon.Width, assemblerIcon.Height))));

            int moduleCount = permutation.Modules.Count;
            int moduleRows = (int)Math.Ceiling(moduleCount / 2d);
            int moduleSize = (int)Math.Min(iconSize / moduleRows, iconSize / (2 - moduleCount % 2)) - 2;

            double x;
            if (moduleCount == 1)
                x = (iconSize - moduleSize) / 2;
            else
                x = (iconSize - moduleSize - moduleSize) / 2;
            double y = (iconSize - (moduleSize * moduleRows)) / 2;

            for (int i = 0, r = 0; r < moduleRows; ++r) {
                dg.Children.Add(new ImageDrawing(
                    permutation.Modules[i].Icon, new Rect(x, y + (r * moduleSize), moduleSize, moduleSize)));

                ++i;
                if (i < permutation.Modules.Count) {
                    dg.Children.Add(new ImageDrawing(
                        permutation.Modules[i].Icon, new Rect(x + moduleSize, y + (r * moduleSize), moduleSize, moduleSize)));
                    ++i;
                }
            }

            return new DrawingImage(dg);
        }

        private string GetPinLabel(Item item, PinKind linkType)
        {
            string line1Format = "{0:0.##}{1}";
            string line2Format = "\n({0:0.##}{1})";
            string line0Format = "[{0:0.##}{1}]\n";
            string finalString;

            string unit = "";

            double actualAmount;
            double suppliedAmount = 0.0;

            if (linkType == PinKind.Input) {
                actualAmount = DisplayedNode.GetConsumeRate(item);
                suppliedAmount = DisplayedNode.GetSuppliedRate(item);
            } else {
                actualAmount = DisplayedNode.GetSupplyRate(item);
            }
            if (Parent.Graph.SelectedAmountType == AmountType.Rate && Parent.Graph.SelectedUnit == RateUnit.PerSecond) {
                unit = "/s";
            } else if (Parent.Graph.SelectedAmountType == AmountType.Rate &&
                       Parent.Graph.SelectedUnit == RateUnit.PerMinute) {
                unit = "/m";
                actualAmount *= 60;
                suppliedAmount *= 60;
            }

            if (linkType == PinKind.Input) {
                finalString = string.Format(line1Format, actualAmount, unit);
                if (DisplayedNode.OverSupplied(item)) {
                    finalString += string.Format(line2Format, suppliedAmount, unit);
                } else if (DisplayedNode.ManualRateNotMet(item)) {
                    float desiredAmount = DisplayedNode.GetDesiredConsumeRate(item);
                    finalString = string.Format(line0Format, desiredAmount, unit) + finalString;
                }
            } else {
                finalString = string.Format(line1Format, actualAmount, unit);
            }

            return finalString;
        }

        private Color? ChoosePinTextColor(Item item, PinKind linkType)
        {
            Color? defaultColor = null;

            if (DisplayedNode.RateType == RateType.Auto)
                return defaultColor;

            PinKind? expectedLinkType = DisplayedNode switch {
                ConsumerNode => PinKind.Input,
                RecipeNode => PinKind.Input,
                PassthroughNode => PinKind.Input,
                SupplyNode => PinKind.Output,
                _ => null
            };

            if (expectedLinkType == null || linkType != expectedLinkType)
                return defaultColor;

            return Color.FromArgb(255, 0, 0, 255);
        }

        private Color ChoosePinFillColor(Item item, PinKind linkType)
        {
            var enough = Colors.White;
            var tooMuch = Color.FromArgb(255, 214, 255, 214);
            var notEnough = Color.FromArgb(255, 255, 226, 214);

            if (linkType == PinKind.Input) {
                if (DisplayedNode.OverSupplied(item))
                    return tooMuch;
                if (DisplayedNode.ManualRateNotMet(item))
                    return notEnough;
            }

            return enough;
        }

        private sealed class PinComparer : IComparer<Pin>
        {
            public int Compare(Pin? x, Pin? y)
            {
                if (ReferenceEquals(x, y))
                    return 0;
                if (y is null)
                    return +1;
                if (x is null)
                    return -1;

                int lhsXOrder = x.GetPinXOrder();
                int rhsXOrder = y.GetPinXOrder();
                int cmp = lhsXOrder.CompareTo(rhsXOrder);
                if (cmp != 0)
                    return cmp;

                return string.Compare(x.Item.Name, y.Item.Name, StringComparison.Ordinal);
            }
        }

        private static readonly PinComparer Comparer = new();

        private void UpdatePinOrder(PinKind? kind = null)
        {
            if (kind is null or PinKind.Input) {
                foreach (var pin in Inputs)
                    pin.ClearXOrder();
                Inputs.StableSort(Comparer);
            }
            if (kind is null or PinKind.Output) {
                foreach (var pin in Outputs)
                    pin.ClearXOrder();
                Outputs.StableSort(Comparer);
            }
        }

        void IContextElement.HandleRightClick(UIElement container)
        {
            BeginEditingNode(container);
        }

        public void BeginEditingNode(UIElement container)
        {
            if (nodeRatePopup != null) {
                nodeRatePopup.IsOpen = false;
                nodeRatePopup = null;
            }

            var popup = PopupUtils.CreatePopup(
                new NodeOptionsViewModel(DisplayedNode, Parent));
            popup.Placement = PlacementMode.Left;
            popup.PlacementTarget = container;
            nodeRatePopup = popup;

            popup.IsOpen = true;
        }

        private string CreateDetails(
            RecipeNode recipeNode, Dictionary<MachinePermutation, double> permutations)
        {
            var buffer = new StringBuilder();
            buffer.AppendFormat("Recipe: {0}", recipeNode.BaseRecipe.FriendlyName);
            buffer.AppendFormat("\n\nBase Time: {0}s", recipeNode.BaseRecipe.Time);

            buffer.Append("\nBase Ingredients:");
            foreach (var kvp in recipeNode.BaseRecipe.Ingredients)
                buffer.AppendFormat("\n  {0} ({1})", kvp.Key.FriendlyName, kvp.Value);

            buffer.Append("\nBase Results:");
            foreach (var kvp in recipeNode.BaseRecipe.Results)
                buffer.AppendFormat("\n  {0} ({1})", kvp.Key.FriendlyName, kvp.Value);

            if (Parent.ShowAssemblers) {
                buffer.Append("\n\nAssemblers:");
                FormatPermutations(buffer, permutations);
            }

            FormatRate(buffer, recipeNode);
            return buffer.ToString();
        }

        private string? CreateDetails(
            SupplyNode supplyNode, Dictionary<MachinePermutation, double> permutations)
        {
            if (supplyNode.Resource == null)
                return null;

            var buffer = new StringBuilder();
            buffer.AppendFormat("Recipe: {0}", supplyNode.Resource.Result.FriendlyName);
            buffer.AppendFormat("\n\nCategory: {0}s", supplyNode.Resource.Category);
            buffer.AppendFormat("\nHardness: {0}s", supplyNode.Resource.Hardness);
            buffer.AppendFormat("\nMining Time: {0}s", supplyNode.Resource.MiningTime);

            if (Parent.ShowMiners) {
                buffer.Append("\n\nMiners:");
                FormatPermutations(buffer, permutations);
            }

            FormatRate(buffer, supplyNode);
            return buffer.ToString();
        }

        private void FormatPermutations(
            StringBuilder buffer, Dictionary<MachinePermutation, double> permutations)
        {
            foreach (var kvp in permutations) {
                var permutation = kvp.Key;

                var assembler = permutation.Assembler;
                buffer.AppendFormat("\n  {0} ({1:F4})", assembler.FriendlyName, kvp.Value);
                foreach (var module in permutation.Modules.NotNull())
                    buffer.AppendFormat("\n    {0}", module.FriendlyName);

                var baseConsumption = assembler.EnergyUsage;
                var actualConsumption =
                    assembler.GetEnergyConsumption(DisplayedNode.BeaconModules.GetConsumptionBonus(), permutation.Modules);
                buffer.AppendFormat("\n  Energy consumption: {0}", actualConsumption.ToShortString("F0"));
                if (actualConsumption != baseConsumption)
                    buffer.AppendFormat(" ({0:+0;−0}%)", ((actualConsumption / baseConsumption) - 1) * 100);

                buffer.AppendFormat("\n  Productivity: {0:+0;−0}%", (DisplayedNode.ProductivityMultiplier() - 1) * 100);

                var baseSpeed = assembler.Speed;
                var actualSpeed = assembler.GetSpeed(DisplayedNode.BeaconModules.GetSpeedBonus(), permutation.Modules);
                buffer.AppendFormat("\n  Speed: {0:F2}", actualSpeed);
                if (Math.Abs(actualSpeed - baseSpeed) > double.Epsilon)
                    buffer.AppendFormat(" ({0:+0;−0}%)", ((actualSpeed / baseSpeed) - 1) * 100);
            }
        }

        private void FormatRate(StringBuilder buffer, ProductionNode node)
        {
            switch (Parent.Graph.SelectedAmountType) {
                case AmountType.FixedAmount:
                    buffer.AppendFormat("\n\nCurrent iterations: {0}", DisplayedNode.ActualRate);
                    break;

                case AmountType.Rate:
                    var unit = Parent.Graph.SelectedUnit == RateUnit.PerMinute ? "m" : "s";

                    buffer.AppendFormat("\n\nCurrent Rate: {0}/{1}",
                        Parent.Graph.SelectedUnit == RateUnit.PerMinute
                            ? DisplayedNode.ActualRate / 60
                            : DisplayedNode.ActualRate,
                        unit);

                    foreach (var item in node.Outputs) {
                        var rate = node.GetSupplyRate(item);
                        buffer.AppendFormat("\n{0} Rate: {1}/{2}", item.FriendlyName, rate, unit);
                    }
                    break;
            }
        }

        protected override GraphElement CreateInstanceCore()
        {
            return new NodeElement(DisplayedNode, Parent);
        }

        protected override void CloneCore(GraphElement source)
        {
            var s = (NodeElement)source;
            Position = s.Position;
            Icon = s.Icon;
            DisplayedNumber = s.DisplayedNumber;
            ShowText = s.ShowText;
            ShowIcon = s.ShowIcon;
            ShowNumber = s.ShowNumber;
            base.CloneCore(source);
        }
    }
}
