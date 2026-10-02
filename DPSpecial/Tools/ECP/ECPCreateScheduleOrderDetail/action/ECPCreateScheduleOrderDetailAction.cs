using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.view;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.viewModel;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
    public class ECPCreateScheduleOrderDetailAction
    {
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPCreateScheduleOrderDetailVM _viewModel;
        private readonly ECPCreateScheduleOrderDetailView _view;

        public ECPCreateScheduleOrderDetailAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;

            _viewModel = new ECPCreateScheduleOrderDetailVM
            {
                Header = CreateSampleHeader(),
                ExportSvgCommand = new RelayCommand(_ExportSvg),
                CancelCommand = new RelayCommand(_Cancel),
            };
            foreach (var row in CreateSampleRows())
                _viewModel.Rows.Add(row);
            _view = new ECPCreateScheduleOrderDetailView { DataContext = _viewModel };
        }

        public void Execute()
        {
            _view.ShowDialog();
        }

        // Exports the 縦切図 shapes as SVG files named by type (製品番号) into a folder the user picks.
        private void _ExportSvg()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Select a folder for the SVG files" };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            try
            {
                var files = ECPOrderDetailSvgExporter.Export(_viewModel.Rows, dialog.SelectedPath);
                IO.ShowInfo($"Exported {files.Count} SVG file(s) to:\n{dialog.SelectedPath}");
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }

        private void _Cancel()
        {
            _view.Close();
        }

        // Placeholder content so the screen can be reviewed; replace with data read from the model once the logic is defined.
        private static ECPOrderDetailHeaderModel CreateSampleHeader()
        {
            return new ECPOrderDetailHeaderModel
            {
                PropertyRegNo = "B260000604", OrderNo = "1262702941",
                SalesStaffCode = "19860571", SalesStaffName = "田川 雅浩",
                Status = "納期回答完了",
                DealerCode = "0000027555", DealerName = "住友林業株式会社",
                DealerBranch = "木材建材事業本部大阪営業部 ｿﾘｭｰｼｮﾝｸﾞﾙｰﾌﾟ",
                PropertyName = "株式会社ｺﾍﾞﾙｺﾛｼﾞｽﾃｨｸｽ 加古川地区新事務所建設工事",
                PropertyDetail = "2F②工区-ﾌﾗｯﾄ",
                PostalCode = "675-0137", PrefectureCode = "28", PrefectureName = "兵庫県",
                Address = "加古川市金沢町35番",
                DesignName = "鹿島建設㈱", ContractorCode = "8000000002", ContractorName = "鹿島建設",
                DesiredDate = "2026/10/02",
                UseCode = "01", UseName = "外壁",
                FactoryCode = "320", FactoryName = "市川工場",
                CategoryCode = "105", CategoryName = "ﾌﾗｯﾄﾊﾟﾈﾙ60",
                FaceCode = "02", FaceName = "【表】研磨【裏】普通",
                GasketOn = true, VinylOn = false,
                HardwareCode = "ZZZ", HardwareName = "金物ｵｰﾀﾞｰ作成しない",
                SpecCode = "03", SpecName = "現場塗装",
                DeliveryMethodCode = "01", DeliveryMethodName = "現場納入",
                VehicleCode = "03", VehicleName = "10ﾄﾝ車",
                VehicleTypeCode = "01", VehicleTypeName = "平車",
                LoadWeight = "1000", LoadHeight = "7", MapOn = false,
                ConstructionDrawingNo = "99999",
                ActualCount = "59", ExchangeCount = "398.423",
                BaseArea = "63.300", BaseWeight = "4,003",
                WorkArea = "63.154", WorkWeight = "3,994", TotalLength = "106.545",
            };
        }

        private static List<ECPOrderDetailRowModel> CreateSampleRows()
        {
            const string flat = "ﾌﾗｯﾄﾊﾟﾈﾙ";
            const string ryogaku = "ﾘｮｳｶﾞｸ凸ﾊﾟﾈﾙ";
            var rows = new List<ECPOrderDetailRowModel>
            {
                new() { No = "3", WorkNo = "402", ProductName = flat, PartNumber = "MNH-6060A", Length = "4180", Quantity = "1", QuantityCode = "11", ShapeKind = "FlatCut" },
                new() { No = "4", WorkNo = "403", ProductName = flat, PartNumber = "MNH-6045A", Length = "4180", Quantity = "1", QuantityCode = "09", ShapeKind = "RightBlock", Dimension = "415" },
                new() { No = "5", WorkNo = "404", ProductName = flat, PartNumber = "MNH-6060A", Length = "2210", Quantity = "11" },
                new() { No = "6", WorkNo = "405", ProductName = ryogaku, PartNumber = "MNH-6060T", Length = "2210", Quantity = "1", QuantityCode = "82" },
                new() { No = "7", WorkNo = "406", ProductName = flat, PartNumber = "MNH-6060A", Length = "1425", Quantity = "11" },
                new() { No = "8", WorkNo = "407", ProductName = flat, PartNumber = "MNH-6060A", Length = "1425", Quantity = "1", QuantityCode = "11", ShapeKind = "FlatCut" },
                new() { No = "9", WorkNo = "408", ProductName = ryogaku, PartNumber = "MNH-6060T", Length = "1425", Quantity = "1", QuantityCode = "82" },
                new() { No = "10", WorkNo = "409", ProductName = flat, PartNumber = "MNH-6060A", Length = "1065", Quantity = "4" },
                new() { No = "11", WorkNo = "410", ProductName = flat, PartNumber = "MNH-6060A", Length = "935", Quantity = "4" },
                new() { No = "12", WorkNo = "411", ProductName = flat, PartNumber = "MNH-6060A", Length = "875", Quantity = "11" },
                new() { No = "13", WorkNo = "412", ProductName = ryogaku, PartNumber = "MNH-6060T", Length = "875", Quantity = "1", QuantityCode = "82" },
                new() { No = "14", WorkNo = "413", ProductName = flat, PartNumber = "MNH-6060A", Length = "300", Quantity = "4" },
            };
            for (var i = 0; i < rows.Count; i++)
                rows[i].IsAlternate = i % 2 == 1;
            return rows;
        }
    }
}
