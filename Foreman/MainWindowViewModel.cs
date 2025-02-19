namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;
    using Extensions;
    using Microsoft.Win32;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Properties;
    using Views;

    public class MainWindowViewModel : ViewModel, ILogger
    {
        private readonly IMainWindow view;

        private bool isEnabled;
        private ProgressInfo? loadProgress;
        private string? currentGraphFile;
        private List<Item> unfilteredItemList = new();
        private List<Recipe> unfilteredRecipeList = new();
        private Difficulty difficulty;
        private Language? selectedLanguage;
        private Item? selectedItem;
        private bool showAssemblers;
        private bool showMiners;
        private string itemFilterText = string.Empty;
        private string recipeFilterText = string.Empty;
        private AmountType amountType = AmountType.FixedAmount;
        private RateUnit selectedRateUnit = RateUnit.PerSecond;
        private ModuleSelector selectedModuleStrategy = ModuleSelector.None;
        private string luaOutput = string.Empty;

        public MainWindowViewModel(IMainWindow view)
        {
            this.view = view;
            RecentGraphs = new MruCollection<string>();

            var graph = new ProductionGraph();
            GraphViewModel = new ProductionGraphViewModel(graph);

            ExitCommand = new AsyncDelegateCommand(Exit);
            CompleteGraphCommand = new AsyncDelegateCommand(CompleteGraph);
            ClearGraphCommand = new AsyncDelegateCommand(ClearGraph);
            ArrangeNodesCommand = new AsyncDelegateCommand(ArrangeNodes);
            ApplyDefaultModulesToNodesCommand = new AsyncDelegateCommand(ApplyDefaultModulesToNodes);
            NewGraphCommand = new AsyncDelegateCommand(NewGraph);
            SaveGraphCommand = new AsyncDelegateCommand(SaveGraph);
            SaveGraphAsCommand = new AsyncDelegateCommand(SaveGraphAs);
            OpenGraphCommand = new AsyncDelegateCommand(LoadGraph);
            LoadGraphCommand = new AsyncDelegateCommand<string>(LoadGraph);
            ExportImageCommand = new AsyncDelegateCommand(ExportImage);
            ChangeFactorioDirectoryCommand = new AsyncDelegateCommand(ChangeFactorioDirectory);
            ChangeModDirectoryCommand = new AsyncDelegateCommand(ChangeModDirectory);
            ReloadGraphCommand = new AsyncDelegateCommand(OnReloadGraph);
            ReloadDataCommand = new AsyncDelegateCommand(OnReloadData);
            EnableDisableCommand = new AsyncDelegateCommand(OnEnableDisableItems);
            AddItemsCommand = new AsyncDelegateCommand<UIElement>(AddItems, CanAddItems);
            AddRecipesCommand = new AsyncDelegateCommand(AddRecipes, CanAddRecipes);
            ShowLuaLogCommand = new AsyncDelegateCommand(ShowLuaLog);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        public ProgressInfo? LoadProgress
        {
            get => loadProgress;
            set => SetProperty(ref loadProgress, value);
        }

        private ProgressInfo CreateProgress()
        {
            return new ProgressInfo(x => {
                IsEnabled = false;
                LoadProgress = x;
            }, _ => {
                LoadProgress = null;
                IsEnabled = true;
            });
        }

        public ProductionGraphViewModel GraphViewModel { get; }

        public MruCollection<string> RecentGraphs { get; }

        public AsyncDelegateCommand ExitCommand { get; }
        public AsyncDelegateCommand CompleteGraphCommand { get; }
        public AsyncDelegateCommand ClearGraphCommand { get; }
        public AsyncDelegateCommand ArrangeNodesCommand { get; }
        public AsyncDelegateCommand ApplyDefaultModulesToNodesCommand { get; }
        public AsyncDelegateCommand NewGraphCommand { get; }
        public AsyncDelegateCommand SaveGraphCommand { get; }
        public AsyncDelegateCommand SaveGraphAsCommand { get; }
        public AsyncDelegateCommand OpenGraphCommand { get; }
        public AsyncDelegateCommand<string> LoadGraphCommand { get; }
        public AsyncDelegateCommand ExportImageCommand { get; }
        public AsyncDelegateCommand ChangeFactorioDirectoryCommand { get; }
        public AsyncDelegateCommand ChangeModDirectoryCommand { get; }
        public AsyncDelegateCommand ReloadGraphCommand { get; }
        public AsyncDelegateCommand ReloadDataCommand { get; }
        public AsyncDelegateCommand EnableDisableCommand { get; }
        public AsyncDelegateCommand<UIElement> AddItemsCommand { get; }
        public AsyncDelegateCommand AddRecipesCommand { get; }
        public AsyncDelegateCommand ShowLuaLogCommand { get; }

        public ObservableCollection<Language> Languages { get; } = new();

        private string windowTitle = "Foreman";

        public string WindowTitle
        {
            get => windowTitle;
            set => SetProperty(ref windowTitle, value);
        }

        public Language? SelectedLanguage
        {
            get => selectedLanguage;
            set
            {
                if (SetProperty(ref selectedLanguage, value))
                    OnLanguageChanged();
            }
        }

        public ObservableCollection<Item> ItemList { get; } = new();

        public Item? SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }

        public ObservableCollection<Recipe> RecipeList { get; } = new();
        public IList<Recipe> SelectedRecipes { get; } = new ObservableCollection<Recipe>();

        public Difficulty Difficulty
        {
            get => difficulty;
            set
            {
                if (SetProperty(ref difficulty, value))
                    DifficultyChanged().Forget();
            }
        }

        public bool ShowAssemblers
        {
            get => showAssemblers;
            set
            {
                if (SetProperty(ref showAssemblers, value))
                    OnShowAssemblersChanged();
            }
        }

        public bool ShowMiners
        {
            get => showMiners;
            set
            {
                if (SetProperty(ref showMiners, value))
                    OnShowMinersChanged();
            }
        }

        public string ItemFilterText
        {
            get => itemFilterText;
            set
            {
                if (SetProperty(ref itemFilterText, value))
                    OnItemFilterTextChanged();
            }
        }

        public string RecipeFilterText
        {
            get => recipeFilterText;
            set
            {
                if (SetProperty(ref recipeFilterText, value))
                    OnRecipeFilterTextChanged();
            }
        }

        public AmountType AmountType
        {
            get => amountType;
            set
            {
                if (SetProperty(ref amountType, value))
                    OnAmountTypeChanged();
            }
        }

        public RateUnit SelectedRateUnit
        {
            get => selectedRateUnit;
            set
            {
                if (SetProperty(ref selectedRateUnit, value))
                    OnSelectedRateUnitChanged();
            }
        }

        public ModuleSelector SelectedModuleStrategy
        {
            get => selectedModuleStrategy;
            set
            {
                if (SetProperty(ref selectedModuleStrategy, value))
                    OnSelectedModuleStrategyChanged();
            }
        }

        public string LuaOutput
        {
            get => luaOutput;
            set => SetProperty(ref luaOutput, value);
        }

        void ILogger.Log(string format, params object?[] args)
        {
            string item = string.Format(format, args);
            LuaOutput += item.Trim() + '\n';
        }

        public async Task Load()
        {
            //I changed the name of the variable, so this copies the value over for people who are upgrading their Foreman version
            if (Settings.Default.FactorioPath == "" && Settings.Default.FactorioDataPath != "") {
                Settings.Default.FactorioPath = Path.GetDirectoryName(Settings.Default.FactorioDataPath);
                Settings.Default.FactorioDataPath = "";
            }

            if (!Directory.Exists(Settings.Default.FactorioPath)) {
                foreach (string defaultPath in new[] {
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)")!, "Factorio"),
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "Factorio"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Applications",
                        "factorio.app", "Contents")
                }) //Not actually tested on a Mac
                {
                    if (Directory.Exists(defaultPath)) {
                        Settings.Default.FactorioPath = defaultPath;
                        Settings.Default.Save();
                        break;
                    }
                }
            }

            if (!Directory.Exists(Settings.Default.FactorioPath)) {
                var dialog = new DirectoryChooserDialog("", "Select Factorio directory");
                if (dialog.ShowDialog(view) == true) {
                    Settings.Default.FactorioPath = dialog.SelectedPath;
                    Settings.Default.Save();
                } else {
                    view.Close();
                    return;
                }
            }

            if (!Directory.Exists(Settings.Default.FactorioModPath)) {
                if (Directory.Exists(Path.Combine(Settings.Default.FactorioPath!, "mods"))) {
                    Settings.Default.FactorioModPath =
                        Path.Combine(Settings.Default.FactorioPath!, "mods");
                } else {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var modsPath = Path.Combine(appData, "factorio", "mods");
                    if (Directory.Exists(modsPath)) {
                        Settings.Default.FactorioModPath = modsPath;
                    }
                }
            }

            Settings.Default.EnabledMods ??= new StringCollection();
            Settings.Default.EnabledAssemblers ??= new StringCollection();
            Settings.Default.EnabledMiners ??= new StringCollection();
            Settings.Default.EnabledModules ??= new StringCollection();

            DataCache.Current.Logger = this;

            switch (Settings.Default.FactorioDifficulty) {
                case "normal":
                    DataCache.Current.Difficulty = "normal";
                    Difficulty = Difficulty.Normal;
                    break;
                case "expensive":
                    DataCache.Current.Difficulty = "expensive";
                    Difficulty = Difficulty.Expensive;
                    break;
                default:
                    Settings.Default.FactorioDifficulty = "normal";
                    Settings.Default.Save();
                    DataCache.Current.Difficulty = "normal";
                    Difficulty = Difficulty.Normal;
                    break;
            }

            ShowAssemblers = GraphViewModel.ShowAssemblers = Settings.Default.ShowAssemblers;
            ShowMiners = GraphViewModel.ShowMiners = Settings.Default.ShowMiners;
            SelectedModuleStrategy = ModuleSelector.FromName(Settings.Default.DefaultModuleStrategy);

            Settings.Default.RecentGraphs ??= new StringCollection();
            RecentGraphs.SetAll(Settings.Default.RecentGraphs.OfType<string>());

            using (var progress = CreateProgress())
                await Task.Run(() => DataCache.Reload(progress));

            var languages = DataCache.Current.Languages;
            Languages.AddRange(languages);
            SelectedLanguage = languages.FirstOrDefault(l => l.Name == Settings.Default.Language) ??
                               languages.FirstOrDefault(l => l.Name == "en") ??
                               languages.First();

            UpdateControlValues();
        }

        private void LoadItemList()
        {
            unfilteredItemList = DataCache.Current.Items.Values.ToList();
            unfilteredItemList.StableSortBy(x => x.FriendlyName);

            ItemList.Clear();
            ItemList.AddRange(unfilteredItemList);
        }

        private void LoadRecipeList()
        {
            unfilteredRecipeList = DataCache.Current.Recipes.Values.ToList();
            unfilteredRecipeList.StableSortBy(x => x.FriendlyName);

            RecipeList.Clear();
            RecipeList.AddRange(unfilteredRecipeList);
        }

        private bool CanAddItems(UIElement? source)
        {
            return SelectedItem != null;
        }

        private async Task AddItems(UIElement? source)
        {
            foreach (Item item in new[] { SelectedItem! }) {
                var itemSupplyOption = new ItemChoice(item, "Create infinite supply node", item.FriendlyName);
                var itemOutputOption = new ItemChoice(item, "Create output node", item.FriendlyName);

                var optionList = new List<Choice>();
                optionList.Add(itemOutputOption);
                foreach (Recipe recipe in DataCache.Current.RecipesSupplying(item)) {
                    optionList.Add(new RecipeChoice(recipe,
                        $"Create '{recipe.FriendlyName}' recipe node", recipe.FriendlyName));
                }
                optionList.Add(itemSupplyOption);

                foreach (Recipe recipe in DataCache.Current.RecipesConsuming(item)) {
                    optionList.Add(new RecipeChoice(recipe,
                        $"Create '{recipe.FriendlyName}' recipe node", recipe.FriendlyName));
                }

                var c = await optionList.ChooseAsync(source, PlacementMode.Right);
                if (c != null) {
                    NodeElement newElement;

                    if (c == itemSupplyOption) {
                        newElement = new NodeElement(SupplyNode.Create(item, GraphViewModel.Graph), GraphViewModel);
                    } else if (c is RecipeChoice rc) {
                        newElement = new NodeElement(
                            RecipeNode.Create(rc.Recipe, GraphViewModel.Graph), GraphViewModel);
                    } else if (c == itemOutputOption) {
                        newElement = new NodeElement(ConsumerNode.Create(item, GraphViewModel.Graph), GraphViewModel);
                    } else {
                        continue;
                    }

                    newElement.Update();
                    newElement.Position = GraphViewModel.ActualViewbox.GetCenter();

                    GraphViewModel.Elements.Add(newElement);
                    GraphViewModel.Graph.UpdateNodeValues(newElement.DisplayedNode);
                }
            }
        }

        private void OnAmountTypeChanged()
        {
            GraphViewModel.Graph.SelectedAmountType = AmountType;
            GraphViewModel.Graph.UpdateNodeValues();
        }

        private void OnSelectedRateUnitChanged()
        {
            GraphViewModel.Graph.SelectedUnit = SelectedRateUnit;
            GraphViewModel.Graph.UpdateNodeValues();
        }

        private void OnSelectedModuleStrategyChanged()
        {
            Settings.Default.DefaultModuleStrategy = SelectedModuleStrategy.Name;
            GraphViewModel.Graph.SelectedModuleStrategy = SelectedModuleStrategy;
            GraphViewModel.Graph.UpdateNodeValues();
        }

        private Task Exit()
        {
            Application.Current.Shutdown();
            return Task.CompletedTask;
        }

        private Task CompleteGraph()
        {
            GraphViewModel.Graph.LinkUpAllInputs();
            GraphViewModel.Graph.UpdateNodeValues();
            GraphViewModel.AddRemoveElements();
            GraphViewModel.PositionNodes();
            return Task.CompletedTask;
        }

        private Task ClearGraph()
        {
            GraphViewModel.Graph.Nodes.Clear();
            GraphViewModel.Elements.Clear();
            return Task.CompletedTask;
        }

        private void OnShowAssemblersChanged()
        {
            Settings.Default.ShowAssemblers = ShowAssemblers;
            Settings.Default.Save();
            GraphViewModel.ShowAssemblers = Settings.Default.ShowAssemblers;
            GraphViewModel.Graph.UpdateNodeValues();
        }

        private Task ExportImage()
        {
            var dialog = new ImageExportDialog(GraphViewModel);
            dialog.ShowDialog(view.Handle);
            return Task.CompletedTask;
        }

        private void OnShowMinersChanged()
        {
            Settings.Default.ShowMiners = ShowMiners;
            Settings.Default.Save();
            GraphViewModel.ShowMiners = Settings.Default.ShowMiners;
            GraphViewModel.Graph.UpdateNodeValues();
        }

        private void OnItemFilterTextChanged()
        {
            var items = unfilteredItemList
                .Where(i => i.FriendlyName.IndexOf(ItemFilterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(i => i.FriendlyName);
            ItemList.Clear();
            ItemList.AddRange(items);
        }

        private async Task ChangeFactorioDirectory()
        {
            var dialog = new DirectoryChooserDialog(Settings.Default.FactorioPath, "Select Factorio directory");
            dialog.Title = "Locate the Factorio directory";
            if (dialog.ShowDialog(view.Handle) != true)
                return;

            Settings.Default.FactorioPath = dialog.SelectedPath;
            Settings.Default.Save();

            await ReloadGraph();
        }

        private async Task ChangeModDirectory()
        {
            var dialog = new DirectoryChooserDialog(Settings.Default.FactorioModPath, "Select Factorio mods directory");
            dialog.Title = "Locate the mods directory";
            if (dialog.ShowDialog(view) != true)
                return;

            Settings.Default.FactorioModPath = dialog.SelectedPath;
            Settings.Default.Save();

            await ReloadGraph();
        }

        private async Task OnReloadGraph()
        {
            await ReloadGraph();
        }

        private async Task ReloadGraph()
        {
            var savedGraph = JObject.Parse(JsonConvert.SerializeObject(GraphViewModel));
            await GraphViewModel.LoadFromJson(savedGraph, ReloadData);
            UpdateControlValues();
        }

        private Task NewGraph()
        {
            ClearGraph();
            currentGraphFile = null;
            UpdateTitle();
            return Task.CompletedTask;
        }

        private Task SaveGraph()
        {
            if (currentGraphFile == null)
                return SaveGraphAs();

            SaveGraphToFile(currentGraphFile);
            return Task.CompletedTask;
        }

        private Task SaveGraphAs()
        {
            var dialog = new SaveFileDialog();
            dialog.DefaultExt = ".json";
            dialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            dialog.AddExtension = true;
            dialog.OverwritePrompt = true;
            dialog.FileName = currentGraphFile ?? "flowchart.json";
            if (dialog.ShowDialog(view) != true)
                return Task.CompletedTask;

            currentGraphFile = dialog.FileName;
            UpdateTitle();
            SaveGraphToFile(currentGraphFile);
            return Task.CompletedTask;
        }

        private void SaveGraphToFile(string filePath)
        {
            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            serializer.ContractResolver = new DefaultContractResolver {
                IgnoreSerializableAttribute = true
            };
            var writer = new JsonTextWriter(new StreamWriter(filePath));
            try {
                serializer.Serialize(writer, GraphViewModel);
                RecentGraphs.Add(filePath);
            } catch (Exception exception) {
                MessageBox.Show("Could not save this file. See log for more details");
                ErrorLogging.LogLine($"Error saving file '{filePath}'. Error: '{exception.Message}'");
            } finally {
                writer.Close();
            }
        }

        private async Task LoadGraph()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(view) != true)
                return;

            await LoadGraph(dialog.FileName);
        }

        private async Task LoadGraph(string? filePath)
        {
            if (filePath == null)
                return;

            try {
                var text = await File.ReadAllTextAsync(filePath);
                await GraphViewModel.LoadFromJson(
                    JObject.Parse(text), ReloadData);

                currentGraphFile = filePath;
                RecentGraphs.Add(filePath);
            } catch (Exception exception) {
                currentGraphFile = null;
                MessageBox.Show("Could not load this file. See log for more details");
                ErrorLogging.LogLine($"Error loading file '{filePath}'. Error: '{exception.Message}'");
            }

            UpdateTitle();
            UpdateControlValues();
        }

        private async Task OnReloadData()
        {
            using (var progress = CreateProgress())
                await ReloadData(progress);
            UpdateControlValues();
        }

        private async Task ReloadData()
        {
            using var progress = CreateProgress();
            await ReloadData(progress);
        }

        private async Task ReloadData(IProgress<string> progress)
        {
            var mods = DataCache.Current.Mods.Where(m => m.Enabled).Select(m => m.Name).ToList();
            await Task.Run(() => DataCache.Reload(mods, progress));
        }

        private async Task OnEnableDisableItems()
        {
            var dialog = new EnableDisableItemsDialog();
            dialog.ShowDialog(view);
            SaveEnabledObjects();

            if (dialog.ModsChanged) {
                using (var progress = CreateProgress())
                    await ReloadData(progress);
                await ReloadGraph();
            }

            GraphViewModel.Graph.UpdateNodeValues();
            GraphViewModel.UpdateNodes();
        }

        private void SaveEnabledObjects()
        {
            Settings.Default.EnabledMods.Clear();
            Settings.Default.EnabledAssemblers.Clear();
            Settings.Default.EnabledMiners.Clear();
            Settings.Default.EnabledModules.Clear();

            Settings.Default.EnabledMods.AddRange(DataCache.Current.Mods
                .Select(m => m.Name + "|" + m.Enabled).ToArray());
            Settings.Default.EnabledAssemblers.AddRange(DataCache.Current.Assemblers.Values
                .Select(a => a.Name + "|" + a.Enabled).ToArray());
            Settings.Default.EnabledMiners.AddRange(DataCache.Current.Miners.Values
                .Select(m => m.Name + "|" + m.Enabled).ToArray());
            Settings.Default.EnabledModules.AddRange(DataCache.Current.Modules.Values
                .Select(m => m.Name + "|" + m.Enabled).ToArray());

            Settings.Default.Save();
        }

        private void OnLanguageChanged()
        {
            if (SelectedLanguage == null)
                return;

            string newLocale = SelectedLanguage.Name;
            Task.Run(async () => await DataCache.Current.ChangeLocaleAsync(newLocale));

            GraphViewModel.UpdateNodes();
            UpdateControlValues();

            Settings.Default.Language = newLocale;
            Settings.Default.Save();
        }

        private void UpdateTitle()
        {
            if (currentGraphFile != null)
                WindowTitle = $"Foreman - {currentGraphFile}";
            else
                WindowTitle = "Foreman";
        }

        private void UpdateControlValues()
        {
            LoadItemList();
            LoadRecipeList();
        }

        private Task ArrangeNodes()
        {
            GraphViewModel.PositionNodes();
            return Task.CompletedTask;
        }

        private Task ApplyDefaultModulesToNodes()
        {
            var graph = GraphViewModel.Graph;

            foreach (var node in graph.Nodes.OfType<EffectableNode>())
                node.Modules = ModuleSelector.Default;

            graph.UpdateNodeValues();

            return Task.CompletedTask;
        }

        private void OnRecipeFilterTextChanged()
        {
            var recipes = unfilteredRecipeList
                .Where(i => i.FriendlyName.IndexOf(RecipeFilterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(i => i.FriendlyName);

            RecipeList.Clear();
            RecipeList.AddRange(recipes);
        }

        private bool CanAddRecipes()
        {
            return SelectedRecipes.Count > 0;
        }

        private Task AddRecipes()
        {
            foreach (Recipe recipe in SelectedRecipes) {
                Point position = GraphViewModel.ActualViewbox.GetCenter();

                var newElement = new NodeElement(
                    RecipeNode.Create(recipe, GraphViewModel.Graph), GraphViewModel);
                newElement.Update();
                newElement.Position = position;
                GraphViewModel.Elements.Add(newElement);
                GraphViewModel.Graph.UpdateNodeValues(newElement.DisplayedNode);
            }

            return Task.CompletedTask;
        }

        private async Task DifficultyChanged()
        {
            var currentDifficulty = DataCache.Current.Difficulty;

            if (Difficulty == Difficulty.Normal)
                DataCache.Current.Difficulty = "normal";
            else if (Difficulty == Difficulty.Expensive)
                DataCache.Current.Difficulty = "expensive";

            if (currentDifficulty == DataCache.Current.Difficulty)
                return;

            Settings.Default.FactorioDifficulty = DataCache.Current.Difficulty;
            Settings.Default.Save();

            await ReloadGraph();
        }

        private Window? luaLogWindow;

        private Task ShowLuaLog()
        {
            if (luaLogWindow == null) {
                var textBox = new TextBox {
                    IsReadOnly = true,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.White,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                };
                BindingOperations.SetBinding(
                    textBox, TextBox.TextProperty,
                    new Binding(nameof(LuaOutput)) {
                        Mode = BindingMode.OneWay
                    });

                var window = new Window {
                    Owner = Application.Current.MainWindow,
                    DataContext = this,
                    Content = textBox,
                    ResizeMode = ResizeMode.CanResizeWithGrip,
                    Title = "Lua Log",
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Width = 700,
                    Height = 800,
                    MinHeight = 100,
                    MinWidth = 100
                };
                window.Closed += (_, _) => luaLogWindow = null;
                window.Show();

                luaLogWindow = window;
            }

            return Task.CompletedTask;
        }
    }
}
