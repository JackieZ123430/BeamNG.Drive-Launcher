using System.Windows.Controls;

namespace BeamNGLauncher.Pages
{
    public partial class ModsPage : UserControl
    {
        public ModsPage()
        {
            InitializeComponent();
        }

        public TextBlock ModsPathText => ModsPathTextBlock;
        public ListBox ModsList => ModsListBox;
        public TextBlock SelectedModNameText => SelectedModNameTextBlock;
        public TextBlock SelectedModSizeText => SelectedModSizeTextBlock;
        public TextBlock SelectedModVehicleText => SelectedModVehicleTextBlock;
        public TextBlock SelectedModPngCountText => SelectedModPngCountTextBlock;
        public ListBox ModPngList => ModPngListBox;
        public Button RefreshModsButtonControl => RefreshModsButton;
        public Button InstallZipButtonControl => InstallZipButton;
        public Button OpenModsFolderButtonControl => OpenModsFolderButton;
    }
}
