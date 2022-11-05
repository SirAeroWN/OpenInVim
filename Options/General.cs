using Community.VisualStudio.Toolkit;

using System.ComponentModel;

namespace OpenInVim
{
    internal partial class OptionsProvider
    {
        // Register the options with these attributes on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "OpenIn", "General", 0, 0, true)]
        // [ProvideProfile(typeof(OptionsProvider.GeneralOptions), "OpenIn", "General", 0, 0, true)]
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    public class General : BaseOptionModel<General>
    {
        [Category("Options")]
        [DisplayName("Absolute path to Vim")]
        [Description("An absolute path the the Vim executable you want to launch.")]
        public string PathToVim { get; set; }

        [Category("Options")]
        [DisplayName("Arguments")]
        [Description("Arguments to pass to Vim. $line$ and $col$ will be replaced by the current line and column number respectively.")]
        [DefaultValue("+call cursor($line$,$col$)")]
        public string UserArguments { get; set; } = "+call cursor($line$,$col$)";


        [Category("Options")]
        [DisplayName("Show in same window")]
        [Description("Run in same window")]
        [DefaultValue(false)]
        public bool SameWindow { get; set; } = false;
        [Category("Options")]
        [DisplayName("Show in new tab for if same window")]
        [Description("Run in same window new tab")]
        [DefaultValue(false)]
        public bool NewTab { get; set; } = false;
    }
}
