namespace Foreman
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public class Connector : GraphElement
    {
        public Connector(NodeLink displayedLink, Pin? source, Pin? destination)
        {
            DisplayedLink = displayedLink;
            Source = source;
            Destination = destination;
            FillColor = DataCache.IconAverageColor(displayedLink.Item.Icon);
        }

        public override bool IsDraggable => false;
        public override bool IsSelectable => true;

        protected override GraphElement CreateInstanceCore()
        {
            return new Connector(DisplayedLink, Source, Destination);
        }

        public NodeLink DisplayedLink { get; }

        public Pin? Source
        {
            get;
            set
            {
                if (field == value)
                    return;

                if (field != null) {
                    field.RemoveConnector(this);
                    field.HotspotUpdated -= OnSourceHotspotUpdated;
                }

                Pin? oldSource = field;
                field = value;

                if (field != null) {
                    field.AddConnector(this);
                    field.HotspotUpdated += OnSourceHotspotUpdated;
                    SourceHotspot = field.Hotspot;
                }

                oldSource?.RaiseConnectionChanged();
                RaisePropertyChanged();
                OnConnectionChanged();
            }
        }

        public Pin? Destination
        {
            get;
            set
            {
                if (field == value)
                    return;

                if (field != null) {
                    field.RemoveConnector(this);
                    field.HotspotUpdated -= OnDestinationHotspotUpdated;
                }

                Pin? oldDestination = field;
                field = value;

                if (field != null) {
                    field.AddConnector(this);
                    field.HotspotUpdated += OnDestinationHotspotUpdated;
                    DestinationHotspot = field.Hotspot;
                }

                oldDestination?.RaiseConnectionChanged();
                RaisePropertyChanged();
                OnConnectionChanged();
            }
        }

        public Point SourceHotspot
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    ComputeConnectorPoints();
            }
        }

        public Point DestinationHotspot
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    ComputeConnectorPoints();
            }
        }

        public PointCollection? Points
        {
            get;
            set => SetProperty(ref field, value);
        }

        public Color FillColor
        {
            get;
            set => SetProperty(ref field, value);
        }

        public event EventHandler<EventArgs>? ConnectionChanged;

        private void OnConnectionChanged()
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
            Source?.RaiseConnectionChanged();
            Destination?.RaiseConnectionChanged();
        }

        private void OnSourceHotspotUpdated(object? sender, EventArgs e)
        {
            SourceHotspot = Source!.Hotspot;
        }

        private void OnDestinationHotspotUpdated(object? sender, EventArgs e)
        {
            DestinationHotspot = Destination!.Hotspot;
        }

        private void ComputeConnectorPoints()
        {
            if (Source != null && Destination!= null && Source.Node == Destination.Node) {
                var computedPoints = new PointCollection {
                    SourceHotspot,
                    SourceHotspot + new Vector(-Source.Node.RenderWidth, -250),
                    DestinationHotspot + new Vector(-Source.Node.RenderWidth, 250),
                    DestinationHotspot
                };
                computedPoints.Freeze();
                Points = computedPoints;
            } else {
                var computedPoints = new PointCollection { SourceHotspot, DestinationHotspot };
                computedPoints.Freeze();
                Points = computedPoints;
            }
        }
    }
}
