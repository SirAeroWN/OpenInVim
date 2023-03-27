using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Project = EnvDTE.Project;

namespace OpenInVim
{
    [Command(PackageIds.MyCommand)]
    internal sealed class OpenInVimCommand : BaseCommand<OpenInVimCommand>
    {
        /// <summary>
        /// Used to get info about the current state of VS
        /// https://learn.microsoft.com/en-us/dotnet/api/envdte.dte?view=visualstudiosdk-2022
        /// </summary>
        private static DTE2 dte;

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                await this.Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                // get DTE
                dte ??= (DTE2)await this.Package.GetServiceAsync(typeof(DTE));

                if (dte == null)
                {
                    throw new Exception("Can't get DTE object");
                }

                // establish our path to vim
                General settings = await General.GetLiveInstanceAsync();
                string path = settings.PathToVim;
                if (path == null)
                {
                    path = "gvim";
                    //await new MessageBox().ShowAsync("Path to Vim cannot be empty", icon: OLEMSGICON.OLEMSGICON_CRITICAL, buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    //return;
                }
                else if (!new FileInfo(path).Exists)
                {
                    await new MessageBox().ShowAsync($"No such file {path}", icon: OLEMSGICON.OLEMSGICON_CRITICAL, buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }

                // set the default line and column
                int line = 1;
                int col = 1;

                // our list of files to open
                List<string> files = new();

                // Finding the file path depends on whether we're in the editor or solutions explorer
                switch (dte.ActiveWindow.Type)
                {
                    case vsWindowType.vsWindowTypeDocument:
                    {
                        TextSelection ts = dte.ActiveWindow.Selection as TextSelection;
                        line = ts == null ? line : ts.CurrentLine;
                        col = ts == null ? col : ts.CurrentColumn;

                        files.Add(dte.ActiveDocument.FullName.Replace("\\", "/"));
                        break;
                    }
                    case vsWindowType.vsWindowTypeSolutionExplorer:
                    {
                        // Check for multiple items selected, and show a warning if there are, then default to using the first one
                        UIHierarchyItem[] selectedFileHierarchyItems = (dte.ActiveWindow.Object as UIHierarchy).SelectedItems as UIHierarchyItem[];

                        foreach (UIHierarchyItem selectedFileHierarchyItem in selectedFileHierarchyItems)
                        {
                            if ((selectedFileHierarchyItem.Object as ProjectItem) != null)
                            {
                                // We're going to find the "FullPath" property in the object's properties and use that
                                Properties selectedFileProperties = ((ProjectItem)selectedFileHierarchyItem.Object).Properties;
                                foreach (object property in selectedFileProperties)
                                {
                                    Property castedProperty = property as Property;
                                    if (castedProperty != null && castedProperty.Name == "FullPath")
                                    {
                                        files.Add((castedProperty.Value as string).Replace("\\", "/"));
                                        break;
                                    }
                                }
                            }
                            else if ((selectedFileHierarchyItem.Object as Project) != null)
                            {
                                // We can just use the FullName attribute on the Project object
                                Project selectedProject = (Project)selectedFileHierarchyItem.Object;
                                files.Add(selectedProject.FullName.Replace("\\", "/"));
                            }
                        }

                        break;
                    }
                }

                string args = settings.UserArguments;
                if (args != null && args.Length > 0)
                {
                    args = $"\"{args.Replace("$line$", line.ToString()).Replace("$col$", col.ToString())}\"";
                }

                System.Diagnostics.Process process = new();
                process.StartInfo.FileName = path; // @"C:\Tools\Neovim5\bin\nvim-qt.exe";
                                                   //process.StartInfo.Arguments = $"\"+call cursor({line},{col})\" \"{doc.FilePath}\"";
                files = files.Select(f => $"\"{f}\"").ToList();
                process.StartInfo.Arguments = $"{args} {string.Join(" ", files)}";
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.Start();
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }
    }
}
