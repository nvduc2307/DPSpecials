using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.MVVM.View;
using DPSpecial.MVVM.ViewModel;
using DPSpecial.Utils;
using DPtools.Utils.Families;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;

namespace DPSpecial.Tools.ECP.ECPFamilyImport
{
    [Transaction(TransactionMode.Manual)]
    public class ECPFamilyImportCmd : IExternalCommand
    {
        private ProgressVM _progressVM;
        private ProgressView _progressView;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "ECPFamilyImportCmd"))
            {
                tsg.Start();
                try
                {
                    _progressVM = new ProgressVM() { PercenComplete = 0 };
                    _progressVM.UpdateProgressAction = _UpdateProgressAction;
                    _progressView = new ProgressView() { DataContext = _progressVM };
                    LoadECPFamily(document);
                    tsg.Assimilate();
                    IO.ShowInfo("Complete!");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
                catch (Exception ex)
                {
                    IO.ShowWarning(ex.Message);
                    tsg.RollBack();
                    result = Result.Failed;
                }
            }
            return result;

        }
        private void _UpdateProgressAction(int valueProgress)
        {
            _progressVM.PercenComplete = valueProgress;
            _progressView.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
        }
        private void LoadECPFamily(Document document)
        {
            var dir = $"{PathHelper.Families}\\ECP";
            var files = GetFilePath(dir);
            if (!files.Any()) return;
            var count = files.Count;
            var per = 100 / (count - 1);
            _progressView.Show();
            using (var ts = new Transaction(document, "LoadECPFamily"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                foreach (var rvt in files)
                {
                    try
                    {
                        var index = files.IndexOf(rvt);
                        FamiliesHelper.LoadFamily(document, rvt);
                        _progressVM.Updateprogress(index * per);
                    }
                    catch (Exception)
                    {
                    }
                }
                ts.Commit();
            }
            _progressView.Close();
        }
        private List<string> GetFilePath(string dir)
        {
            var result = new List<string>();
            if (!Directory.Exists(dir)) return result;
            var files = Directory.GetFiles(dir);
            if (files.Any())
            {
                var fileRvts = files
                    .Where(x => x.Contains(".rfa") && x.Count(x => x == '.') == 1)
                    .ToList();
                if (fileRvts.Any())
                    result.AddRange(fileRvts);
            }
            var folders = Directory.GetDirectories(dir);
            foreach (var folder in folders)
            {
                var subFiles = GetFilePath(folder);
                if (subFiles.Any())
                    result.AddRange(subFiles);
            }
            return result;
        }
    }
}
