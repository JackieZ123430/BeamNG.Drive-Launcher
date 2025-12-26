using System.Windows.Controls;

namespace BeamNGLauncher.Pages
{
    public partial class LaunchPage : UserControl
    {
        public LaunchPage()
        {
            InitializeComponent();
        }

        public ComboBox GfxCombo => GfxComboBox;
        public CheckBox HeadlessCheck => HeadlessCheckBox;
        public ComboBox LevelCombo => LevelComboBox;
        public ComboBox VehicleCombo => VehicleComboBox;
        public CheckBox ConsoleCheck => ConsoleCheckBox;
        public CheckBox CefDevCheck => CefDevCheckBox;
        public RadioButton CrashDefaultOption => CrashDefaultRadio;
        public RadioButton CrashFullOption => CrashFullRadio;
        public RadioButton CrashNoneOption => CrashNoneRadio;
        public TextBox LuaChunkInput => LuaChunkTextBox;
        public TextBox ExecInput => ExecTextBox;
        public CheckBox LuaStdinCheck => LuaStdinCheckBox;
        public CheckBox LuaDebugCheck => LuaDebugCheckBox;
        public CheckBox TcomCheck => TcomCheckBox;
        public TextBox TportInput => TportTextBox;
        public TextBox TcomListenIpInput => TcomListenIpTextBox;
        public CheckBox TcomDebugCheck => TcomDebugCheckBox;
        public TextBox ExtraArgsInput => ExtraArgsTextBox;
        public Button LaunchButtonControl => LaunchButton;
    }
}
