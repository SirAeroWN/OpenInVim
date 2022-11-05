
using System;
using System.Diagnostics;
using System.IO;

using Community.VisualStudio.Toolkit;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace OpenInVim
{
    [Command(PackageIds.MyCommand)]
    internal sealed class OpenInVimCommand : BaseCommand<OpenInVimCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                await this.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
                DocumentView doc = await VS.Documents.GetActiveDocumentViewAsync();
                var something = (DTE2)(await this.Package.GetServiceAsync(typeof(DTE)));
                var ts = something.ActiveWindow.Selection as TextSelection;

                int line = ts == null ? 1 : ts.CurrentLine;
                int col = ts == null ? 1 : ts.CurrentColumn;

                General settings = await General.GetLiveInstanceAsync();
                string path = settings.PathToVim;
                if (path == null)
                {
                    await new MessageBox().ShowAsync("Path to Vim cannot be empty", icon: OLEMSGICON.OLEMSGICON_CRITICAL, buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }
                else if (!new FileInfo(path).Exists)
                {
                    await new MessageBox().ShowAsync($"No such file {path}", icon: OLEMSGICON.OLEMSGICON_CRITICAL, buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }

                string args = settings.UserArguments;
                args = args.Replace("$line$", line.ToString()).Replace("$col$", col.ToString());
                System.Diagnostics.Process process = new();
                process.StartInfo.FileName = path; // @"C:\Tools\Neovim5\bin\nvim-qt.exe";
                                                   //process.StartInfo.Arguments = $"\"+call cursor({line},{col})\" \"{doc.FilePath}\"";



                if (settings.SameWindow)
                {
                    if (settings.NewTab)
                    {

                        process.StartInfo.Arguments = $"--remote-tab \"{args}\" \"{doc.FilePath}\"";
                    }
                    else
                    {
                        process.StartInfo.Arguments = $"--remote \"{args}\" \"{doc.FilePath}\"";
                    }
                }
                else
                {

                    process.StartInfo.Arguments = $"\"{args}\" \"{doc.FilePath}\"";
                }

                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized;
                process.Start();
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }
    }
}
