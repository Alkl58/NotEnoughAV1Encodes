using ControlzEx.Theming;
using System.Windows.Controls;

namespace NotEnoughAV1Encodes.Views.Tabs
{   
    public partial class Subtitles : Page
    {
        public Subtitles()
        {
            InitializeComponent();
        }

        public void ThemeUpdate(string _theme)
        {
            try { ThemeManager.Current.ChangeTheme(this, _theme); } catch { }
        }
    }
}
